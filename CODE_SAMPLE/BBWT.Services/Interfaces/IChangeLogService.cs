namespace BBWT.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Linq;

    using BBWT.Data.Audit;

    /// <summary>
    /// ChangeLog Service interface
    /// </summary>
    public interface IChangeLogService
    {
        /// <summary>
        /// Add Change Log
        /// </summary>
        /// <param name="changeLog">ChangeLog</param>
        void AddChangeLog(ChangeLog changeLog);

        /// <summary>Create Change Log</summary>
        /// <param name="item">ChangeLog</param>
        void SaveChangeLog(ChangeLog item);

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
        /// Changes parsed collection
        /// </param>
        /// <param name="changesXml">
        /// Changes Xml string
        /// </param>
        void SaveChangeLog(string entityType, int entityId, ChangeLogActionType actionType, List<ChangeLogItem> changes, string changesXml);

        /// <summary>List of Change Logs</summary>
        /// <param name="id">Entity Id</param>
        /// <param name="entityType">Entity Type</param>
        /// <returns>List of logs.</returns>
        IQueryable<ChangeLog> GetEntityChangeLogs(int id, string entityType);

        /// <summary>List of Change Logs</summary>
        /// <returns>List of logs.</returns>
        IQueryable<ChangeLog> GetAllChangeLogs();
    }
}
