namespace BBWT.Services.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Web.Security;

    using BBWT.Data.Audit;
    using BBWT.Data.Membership;
    using BBWT.Domain;
    using BBWT.Services.Exceptions;
    using BBWT.Services.Interfaces;
    using BBWT.Services.Utils;

    using EntityFramework.Audit;
    using EntityFramework.Extensions;

    using WebMatrix.WebData;
    using BBWT.Data.keltbray_piling;

    /// <summary>
    /// Membership Service class
    /// </summary>
    public class MembershipService : IMembershipService
    {
        private const int CUSTOM_PERMISSION_ID = 1000;

        private readonly IDataContext context;

        private readonly IAuditContext auditContext;

        private readonly IPrincipal principal;

        private readonly User currentUser;

        private readonly ICommonDataService dataService;

        private readonly IConfigService configService;

        private readonly AuditLogger audit;


        /// <summary>
        /// Constructs membersip service
        /// </summary>
        /// <param name="ctx">
        /// context injection
        /// </param>
        /// <param name="principal">
        /// current user injection
        /// </param>
        /// <param name="dataService">
        /// Common Data Service
        /// </param>
        /// <param name="configService">
        /// Site Configuration Service
        /// </param>
        /// <param name="auditContext">
        /// The audit Context.
        /// </param>
        public MembershipService(IDataContext ctx, IPrincipal principal, ICommonDataService dataService, IConfigService configService, IAuditContext auditContext)
        {
            Debug.Assert(ctx != null, "Data context should not be null");
            Debug.Assert(principal != null, "User should not be null");

            this.context = ctx;
            this.principal = principal;
            this.dataService = dataService;
            this.configService = configService;

            this.currentUser =
                this.principal.Identity.IsAuthenticated ?
                this.GetUserByName(this.principal.Identity.Name) : null;

            this.auditContext = auditContext;
            this.audit = ((DbContext)this.context).BeginAudit();
        }

        /// <summary>Create permission</summary>
        /// <param name="permission">Permission to create</param>
        public void CreatePermission(Permission permission)
        {
            Debug.Assert(permission != null, "Empty permission can't be created");
            Debug.Assert(permission.Id == 0, "Permission to create should have no identity value");

            var id = this.context.Permissions.Max(p => p.Id);
            permission.Id = Math.Max(id + 1, CUSTOM_PERMISSION_ID);

            this.context.Permissions.Add(permission);
            this.context.Commit();
        }

        /// <summary>Create user</summary>
        /// <param name="user">user to create</param>
        /// <param name="pass">password</param>
        public void CreateUser(User user, string pass = "")
        {
            Debug.Assert(user != null, "Empty user can't be created");
            Debug.Assert(user.Id == 0, "User to create should have no identity value");

            if (!this.context.Users.Any(u => u.Name == user.Name))
            {
                this.context.Users.Add(user);
                this.context.Commit();

                //// save hash of 'password' if empty
                WebSecurity.CreateAccount(user.Name, string.IsNullOrEmpty(pass) ? "b109f3bbbc244eb82441917ed06d618b9008dd09b3befd1b5e07394c706a8bb980b1d7785e5976ec049b46df5f1326af5a2ea6d103fd07c95385ffab0cacbc86" : pass);
                this.SaveChangeLog<User>(user.Id, ChangeLogActionType.Create);
            }
            else
            {
                throw new ValidationException("User already exists.");
            }
        }

        /// <summary>delete permission by id</summary>
        /// <param name="id">permission id</param>
        public void DeletePermission(int id)
        {
            Debug.Assert(id > 0, "Permission should have valid identity");

            if (id < CUSTOM_PERMISSION_ID)
            {
                return;
            }

            var permission = this.context.Permissions.Find(id);
            if (permission == null)
            {
                return;
            }

            this.context.Permissions.Remove(permission);
            this.context.Commit();
        }

        /// <summary>Delete user</summary>
        /// <param name="id">User ID</param>
        public void DeleteUser(int id)
        {
            var user = this.dataService.Find<User>(id);

            // Admin can't be deleted. (User can't be set as admin using Web UI)
            if (user == null || user.Roles.Any(r => r.IsAdmin))
            {
                return;
            }

            this.DeleteAllUserSecurityTickets(id);
            (Membership.Provider as SimpleMembershipProvider).DeleteAccount(user.Name);
            (Membership.Provider as SimpleMembershipProvider).DeleteUser(user.Name, true);

            this.SaveChangeLog<User>(user.Id, ChangeLogActionType.Delete);
        }

        /// <summary>
        /// Get list of current user permissions
        /// </summary>
        /// <returns>List of permissions</returns>
        public List<Permission> GetEffectivePermissions()
        {
            return this.GetEffectivePermissions(this.currentUser.Id);
        }

        /// <summary>
        /// Get list of selected user permissions
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>List of permissions</returns>
        public List<Permission> GetEffectivePermissions(int id)
        {
            var user = this.dataService.Find<User>(id);

            if (user == null)
            {
                return new List<Permission>();
            }

            // Admin has all permissions, except ones disabled by Site Configuration
            if (user.Roles.Any(r => r.IsAdmin))
            {
                return this.context.Permissions.ToList()
                    .FilterBySettings(this.configService.Settings);
            }

            return user.Roles.SelectMany(r => r.Permissions)
                .Distinct().ToList()
                .FilterBySettings(this.configService.Settings);
        }

        /// <summary>Get single permission by code</summary>
        /// <param name="code">Permission code</param>
        /// <returns>Permission</returns>
        public Permission GetPermissionByCode(string code)
        {
            Debug.Assert(!string.IsNullOrEmpty(code), "Permission code should not be empty");

            return this.context.Permissions.FirstOrDefault(p => p.Code == code);
        }

        /// <summary>Get user by login name</summary>
        /// <param name="name">name of user</param>
        /// <returns>user</returns>
        public User GetUserByName(string name)
        {
            return this.context.Users.FirstOrDefault(u => u.Name == name);
        }

        /// <summary>
        /// Get ID of current user
        /// </summary>
        /// <returns>User ID</returns>
        public int GetCurrentUserId()
        {
            return this.currentUser == null ? 0 : this.currentUser.Id;
        }

        /// <summary>
        /// Get current user
        /// </summary>
        /// <returns>User</returns>
        public User GetCurrentUser()
        {
            return this.currentUser;
        }

        /// <summary>Check current user permission</summary>
        /// <param name="permissionCode">Permission code</param>
        /// <returns>true if user has permission</returns>
        public bool HasPermission(string permissionCode)
        {
            if (this.currentUser == null)
            {
                return false;
            }

            return this.HasPermission(this.currentUser.Id, permissionCode);
        }

        /// <summary>Check if user has permission</summary>
        /// <param name="userId">User Id</param>
        /// <param name="permissionCode">Permission code</param>
        /// <returns>true if user has permission</returns>
        public bool HasPermission(int userId, string permissionCode)
        {
            return this.GetEffectivePermissions(userId).Any(p => p.Code == permissionCode);
        }

        /// <summary>Check user permissions</summary>
        /// <param name="userId">User Id</param>
        /// <param name="codes">Permissions codes</param>
        /// <returns>true if user has permission</returns>
        public bool HasAnyPermission(int userId, string[] codes)
        {
            return this.GetEffectivePermissions(userId).Any(p => codes.Contains(p.Code));
        }

        /// <summary>Check user permissions</summary>
        /// <param name="codes">Permissions codes</param>
        /// <returns>true if user has permission</returns>
        public bool HasAnyPermission(string[] codes)
        {
            if (this.currentUser == null)
            {
                return false;
            }

            return this.HasAnyPermission(this.currentUser.Id, codes);
        }

        /// <summary>
        /// Delete all security tickets for user
        /// </summary>
        /// <param name="id">User id</param>
        public void DeleteAllUserSecurityTickets(int id)
        {
            var user = this.dataService.Find<User>(id);

            if (user == null)
            {
                return;
            }

            foreach (var ticket in this.context.UserSecurityTickets)
            {
                this.context.UserSecurityTickets.Remove(ticket);
            }

            this.context.Commit();
        }

        /// <summary>Get user by windows user name</summary>
        /// <param name="name">name of windows account</param>
        /// <returns>user</returns>
        public User GetUserByWinName(string name)
        {
            User result = null;

            if (!string.IsNullOrWhiteSpace(name))
            {
                result = this.context.Users.FirstOrDefault(u => u.WindowsUserName == name);                
            }

            return result;
        }

        /// <summary>
        /// Get site administrator user
        /// </summary>
        /// <returns>Site admin</returns>
        public User GetSiteAdmin()
        {
            return this.context.Users.FirstOrDefault(u => u.Roles.Any(r => r.Type == RoleType.Admin));
        }


        private void SaveChangeLog<T>(int entityId, ChangeLogActionType actionType) where T : class
        {
            this.audit.LastLog.Refresh();
            if (this.audit.LastLog.Entities.Count > 0)
            {
                var changeLog = new ChangeLog
                {
                    EntityType = typeof(T).Name,
                    EntityId = entityId,
                    ActionType = actionType,
                    ChangesXml = this.audit.LastLog.ToXml(),
                    DateTime = DateTime.Now,
                    UserName = this.GetCurrentUser().Name
                };

                this.auditContext.ChangeLogs.Add(changeLog);
                this.auditContext.Commit();

            }
        }
    }
}