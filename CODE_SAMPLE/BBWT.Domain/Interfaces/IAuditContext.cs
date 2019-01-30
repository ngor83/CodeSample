namespace BBWT.Domain
{
    using System;
    using System.Data.Common;
    using System.Data.Entity;

    using BBWT.Data.Audit;
    using BBWT.Domain.Interfaces;

    /// <summary>
    /// Audit context interface
    /// </summary>
    public interface IAuditContext : IDisposable, IGenericRepo
    {
        /// <summary>
        /// Change Logs
        /// </summary>
        IDbSet<ChangeLog> ChangeLogs { get; set; }

        /// <summary>
        /// Current Database Connection
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// Commit changes to database
        /// </summary>
        void Commit();

        /// <summary>
        /// Revert database changes
        /// </summary>
        void Rollback();
    }
}
