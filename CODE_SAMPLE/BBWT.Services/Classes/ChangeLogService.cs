namespace BBWT.Services.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using BBWT.Data.Audit;
    using BBWT.Domain;
    using BBWT.Services.Interfaces;

    /// <summary>
    /// ChangeLog Service class
    /// </summary>
    public class ChangeLogService : IChangeLogService
    {
        private readonly IAuditContext context;

        private readonly IMembershipService membershipService;

        /// <summary>Constructs ChangeLog service</summary>
        /// <param name="ctx">context injection</param>
        /// <param name="membershipService">Membership service</param>
        public ChangeLogService(IAuditContext ctx, IMembershipService membershipService)
        {
            this.context = ctx;
            this.membershipService = membershipService;
        }

        /// <summary>Add Change Log</summary>
        /// <param name="changeLog">Change Log</param>
        public void AddChangeLog(ChangeLog changeLog)
        {
            changeLog.DateTime = DateTime.Now;
            changeLog.UserName = this.membershipService.GetCurrentUser().Name;

            changeLog.ActionType = ChangeLogActionType.GenericLog;

            this.context.ChangeLogs.Add(changeLog);
            this.context.Commit();
        }

        /// <summary>
        /// Save change log.
        /// </summary>
        /// <param name="changeLog">
        /// The change log.
        /// </param>
        public void SaveChangeLog(ChangeLog changeLog)
        {
            if (changeLog.Id == 0)
            {
                this.AddChangeLog(changeLog);
                return;
            }

            this.context.Update<ChangeLog>(
                changeLog.Id,
                log =>
                {
                    log.Changes = changeLog.Changes;
                    if (!string.IsNullOrWhiteSpace(changeLog.ChangesXml))
                    {
                        log.ChangesXml = changeLog.ChangesXml;
                    }
                });
            this.context.Commit();
        }

        /// <summary>
        /// Save Change Log
        /// </summary>
        /// <param name="entityType">
        /// The entity Type.
        /// </param>
        /// <param name="entityId">
        /// The entity Id.
        /// </param>
        /// <param name="actionType">
        /// The action Type.
        /// </param>
        /// <param name="changes">
        /// The changes.
        /// </param>
        /// <param name="changesXml">
        /// The changes Xml.
        /// </param>
        public void SaveChangeLog(string entityType, int entityId, ChangeLogActionType actionType, List<ChangeLogItem> changes, string changesXml)
        {
            var user = this.membershipService.GetCurrentUser();
            var changeLog = new ChangeLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                Changes = changes,
                ChangesXml = changesXml,
                DateTime = DateTime.Now,
                UserName = user != null ? user.Name : "Opera"
            };

            this.context.ChangeLogs.Add(changeLog);
            this.context.Commit();
        }

        /// <summary>List of Change Logs</summary>
        /// <param name="id">Entity Id</param>
        /// <param name="entityType">Entity Type</param>
        /// <returns>List of logs.</returns>
        public IQueryable<ChangeLog> GetEntityChangeLogs(int id, string entityType)
        {
            return this.context.ChangeLogs
                .Where(c => c.EntityId == id && c.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.DateTime);
        }

        /// <summary>
        /// The get all change logs.
        /// </summary>
        /// <returns>
        /// The <see cref="IQueryable"/>.
        /// </returns>
        public IQueryable<ChangeLog> GetAllChangeLogs()
        {
            return this.context.GetAll<ChangeLog>();
        }
    }
}
