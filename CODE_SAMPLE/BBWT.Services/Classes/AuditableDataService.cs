namespace BBWT.Services.Classes
{
    using System;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;

    using BBWT.Data;
    using BBWT.Data.Audit;
    using BBWT.Domain;
    using BBWT.Services.Interfaces;
    using BBWT.Services.Utils;

    using EntityFramework.Audit;
    using EntityFramework.Extensions;

    /// <summary>
    /// Common Data Service class
    /// </summary>
    public class AuditableDataService : IAuditableDataService
    {
        private readonly IDataContext context;

        private readonly IChangeLogService changeLogService;

        private readonly AuditLogger audit;

        /// <summary>Constructs CommonData service</summary>
        /// <param name="ctx">context injection</param>
        /// <param name="changeLogService">change log service</param>
        public AuditableDataService(IDataContext ctx, IChangeLogService changeLogService)
        {
            Debug.Assert(ctx != null, "Data context should not be null");

            this.context = ctx;
            this.changeLogService = changeLogService;

            this.audit = ((DbContext)this.context).BeginAudit();
        }

        /// <summary>
        /// Create Item
        /// </summary>
        /// <typeparam name="T">Source Type</typeparam>
        /// <param name="entity">Item</param>
        /// <returns>Return Type</returns>
        public T Insert<T>(T entity) where T : class
        {
            var result = this.context.Insert(entity);
            var e = result as Entity;
            if (e != null)
            {
                this.SaveChangeLog<T>(e.Id, ChangeLogActionType.Create);
            }

            return result;
        }

        /// <summary>
        /// Get Item
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="x">Param</param>
        /// <returns>Item</returns>
        public T SingleOrDefault<T>(Func<T, bool> x) where T : class
        {
            return this.context.SingleOrDefault(x);
        }

        /// <summary>
        /// Get Items
        /// </summary>
        /// <typeparam name="T">Type</typeparam>        
        /// <returns>Items</returns>
        public IQueryable<T> GetAll<T>() where T : class
        {
            return this.context.GetAll<T>();
        }

        /// <summary>
        /// Get Item By Id
        /// </summary>
        /// <typeparam name="T">Type</typeparam>        
        /// <param name="id">Id</param>
        /// <returns>Item</returns>
        public T Find<T>(object id) where T : class
        {
            return this.context.Find<T>(id);
        }

        /// <summary>
        /// Update Item With Update Strategy
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="id">Id</param>
        /// <param name="updateStrategy">Strategy</param>
        public void Update<T>(object id, Action<T> updateStrategy) where T : class
        {
            this.context.Update(id, updateStrategy);

            this.SaveChangeLog<T>((int)id, ChangeLogActionType.Update);
        }

        /// <summary>
        /// Delete Item By Id
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="id">Id</param>
        public void Delete<T>(object id) where T : class
        {
            this.context.Delete<T>(id);

            this.SaveChangeLog<T>((int)id, ChangeLogActionType.Delete);
        }

        /// <summary>
        /// Delete Item
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Item</param>
        public void Delete<T>(T entity) where T : class
        {
            this.context.Delete(entity);

            var e = entity as Entity;
            if (e != null)
            {
                this.SaveChangeLog<T>(e.Id, ChangeLogActionType.Delete);
            }
        }

        private void SaveChangeLog<T>(int entityId, ChangeLogActionType actionType) where T : class
        {
            this.audit.LastLog.Refresh();
            if (this.audit.LastLog.Entities.Count > 0)
            {
                var items = this.audit.LastLog.ToLogItems();
                // decide if we need xml at all
                // currently commented as still causes unexpected issues with serializing
                // var xml = this.audit.LastLog.ToXml();
                this.changeLogService.SaveChangeLog(typeof(T).Name, entityId, actionType, items, null);
            }
        }
    }
}