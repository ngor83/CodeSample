namespace BBWT.Domain
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.ModelConfiguration;
    using System.Linq;
    using System.Reflection;

    using BBWT.Data.Audit;
    using BBWT.Domain.Migrations;
    using BBWT.Domain.Migrations.AuditContext;

    using Common.Logging;

    /// <summary>
    /// Audit context definition
    /// </summary>
    public class AuditContext : DbContext, IAuditContext
    {
        /// <summary>
        /// Definition of database initialization strategy
        /// </summary>
        static AuditContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AuditContext, AuditConfiguration>());
        }

        /// <summary>
        /// Constructs new instance of <see cref="AuditContext"/>
        /// </summary>
        public AuditContext()
            : base("AuditConnection")
        {
            Database.Initialize(false);

            this.ChangeLogs = this.Set<ChangeLog>();
        }

        /// <summary>
        /// Change Logs.
        /// </summary>
        public IDbSet<ChangeLog> ChangeLogs { get; set; }

        /// <summary>
        /// Current Database Connection
        /// </summary>
        /// <remarks>Be careful with low-level connection object!</remarks>
        public DbConnection Connection 
        {
            get
            {
                return this.Database.Connection;
            }
        }

        /// <summary>
        /// Commit changes
        /// </summary>
        public void Commit()
        {
            this.SaveChanges();
        }

        /// <summary>
        /// Revert changes
        /// </summary>
        public void Rollback()
        {
            var changedEntries = this.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

            foreach (var entry in changedEntries.Where(x => x.State == EntityState.Modified))
            {
                entry.CurrentValues.SetValues(entry.OriginalValues);
                entry.State = EntityState.Unchanged;
            }

            foreach (var entry in changedEntries.Where(x => x.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            foreach (var entry in changedEntries.Where(x => x.State == EntityState.Deleted))
            {
                entry.State = EntityState.Unchanged;
            }
        }

        /// <summary>
        /// Get Item
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="x">Param</param>
        /// <returns>Item</returns>
        public T SingleOrDefault<T>(Func<T, bool> x) where T : class
        {
            return this.Set<T>().SingleOrDefault(x);
        }

        /// <summary>
        /// Get Items
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Items</returns>
        public IQueryable<T> GetAll<T>() where T : class
        {
            return this.Set<T>();
        }

        /// <summary>
        /// Get Item By Id
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="id">Id</param>
        /// <returns>Item</returns>
        public T Find<T>(object id) where T : class
        {
            return this.Set<T>().Find(id);
        }        

        /// <summary>
        /// Create Item
        /// </summary>
        /// <typeparam name="T">Source Type</typeparam>
        /// <param name="entity">Item</param>
        /// <returns>Return Type</returns>
        public T Insert<T>(T entity) where T : class
        {
            var item = this.Set<T>().Add(entity);
            this.Commit();

            return item;
        }

        /// <summary>
        /// Update Item With Update Strategy
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="id">Id</param>
        /// <param name="updateStrategy">Strategy</param>
        public void Update<T>(object id, Action<T> updateStrategy) where T : class
        {
            var entity = this.Find<T>(id);

            if (entity != null)
            {
                updateStrategy(entity);
                this.Commit();
            }
        }

        /// <summary>
        /// Delete Item By Id
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="id">Id</param>
        public void Delete<T>(object id) where T : class
        {
            var entity = this.Find<T>(id);
            this.Delete(entity);
        }

        /// <summary>
        /// Delete Item
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Item</param>
        public void Delete<T>(T entity) where T : class
        {
            if (entity == null)
            {
                return;
            }

            if (this.Entry(entity).State == EntityState.Detached)
            {
                this.Set<T>().Attach(entity);
            }

            this.Set<T>().Remove(entity);
            this.Commit();
        }

        /// <summary>Dispose custom database context</summary>
        /// <param name="disposing">true if both managed/unmanaged resource should be disposed</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (this.ChangeTracker.HasChanges())
                {
                    var changedEntries = this.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();
                   
                    var log = LogManager.GetCurrentClassLogger();
                    log.Error("Database context has unsaved changes");
                    throw new DbUpdateException("Database context has unsaved changes");
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>Define default mapping rules</summary>
        /// <param name="modelBuilder">Default model builder</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChangeLog>().HasKey(t => t.Id);

            modelBuilder.Entity<ChangeLog>()
                .Property(t => t.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("Id");

            modelBuilder.Entity<ChangeLog>()
                .HasMany(p => p.Changes)
                .WithRequired();

            modelBuilder.Entity<ChangeLog>().ToTable("ChangeLogs");

            modelBuilder.Entity<ChangeLogItem>().HasKey(p => p.Id);

            modelBuilder.Entity<ChangeLogItem>()
                .Property(t => t.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("Id");

            modelBuilder.Entity<ChangeLogItem>()
                .Property(t => t.ChangeText)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

            modelBuilder.Entity<ChangeLogItem>().ToTable("ChangeLogItems");

            // this.RegisterConfigurations(modelBuilder);
        }

        /// <summary>Maps registration</summary>
        /// <param name="modelBuilder">Default model builder</param>
        private void RegisterConfigurations(DbModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException("modelBuilder");
            }

            Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(
                        type =>
                        type.BaseType != null && type.BaseType.IsGenericType
                        && type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>))
                    .ToList()
                    .ForEach(
                        type =>
                        {
                            dynamic instance = Activator.CreateInstance(type);
                            modelBuilder.Configurations.Add(instance);
                        });
        }
    }
}