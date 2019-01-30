namespace BBWT.Web.WebAPI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration; 
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Mail;
    using System.Web.Http;

    using AutoMapper;
    using AutoMapper.QueryableExtensions;

    using BBWT.Data.Membership;
    using BBWT.DTO;
    using BBWT.DTO.Membership;
    using BBWT.Services.Exceptions;
    using BBWT.Services.Interfaces;
    using BBWT.Services.Messages;
    using BBWT.Web.Attributes;
    using BBWT.Web.Helpers;

    using WebMatrix.WebData;

    /// <summary>
    /// User management API controller
    /// </summary>
    public class UsersController : ApiController
    {
        private const string ConfigFallbackLanguage = "FallbackLanguage";

        private readonly IMembershipService service;

        private readonly ISecurityService securityService;

        private readonly IMappingEngine mapper;

        private readonly IEmailSender sender;

        private readonly IAuditableDataService dataService;

        /// <summary>Constructs users controller</summary>
        /// <param name="srv">membership service injection</param>
        /// <param name="srvS">security service injection</param>
        /// <param name="mapper">Mapper instance</param>
        /// <param name="sender">Email sender</param>
        /// <param name="dataService">Data Service</param>
        public UsersController(IMembershipService srv, ISecurityService srvS, IMappingEngine mapper, IEmailSender sender, IAuditableDataService dataService)
        {
            this.service = srv;
            this.securityService = srvS;
            this.mapper = mapper;
            this.sender = sender;
            this.dataService = dataService;
        }

        /// <summary>Delete user</summary>
        /// <param name="id">User ID</param>
        [HttpGet]
        [HasPermission(Permissions = "ManageUsers")]
        public void DeleteUser(int id)
        {
            this.service.DeleteUser(id);
        }

        /// <summary>
        /// Get user settings
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>user settings</returns>
        [HttpGet]
        [Authorize] 
        public UserSettingsDto GetUserSettings(int id)
        {         
            var settings = this.dataService.Find<UserSettings>(id);
            return this.mapper.Map<UserSettingsDto>(settings);
        }

        /// <summary>Get list of users</summary>
        /// <returns>List of users</returns>
        [HttpGet]
        [HasPermission(Permissions = "ManageUsers, ManageRoles, ManageGroups")]
        public IQueryable<AccountDTO> GetAllUsers()
        {            
            return this.dataService.GetAll<User>().Project().To<AccountDTO>();
        }

        /// <summary>Get user by id</summary>
        /// <param name="id">User ID</param>
        /// <returns>Users DTO</returns>
        [HttpGet]
        [HasPermission(Permissions = "ManageUsers")]
        public UserDTO GetUserById(int id)
        {
            var user = id == 0
                           ? new User
                                 {
                                     Name = string.Empty,
                                     Groups = new List<Group>(),
                                     Roles = new List<Role>()
                                 }
                            : this.dataService.Find<User>(id);                           
                           
            var userDTO = this.mapper.Map<UserDTO>(user);
            
            var groupsDTO = this.dataService.GetAll<Group>().Project().To<CheckBoxItemDTO>().ToList();
            groupsDTO.ForEach(it => it.IsChecked = user.Groups.Any(p => p.Id == it.Id));
            
            var rolesDTO = this.dataService.GetAll<Role>().Project().To<CheckBoxItemDTO>().ToList();
            rolesDTO.ForEach(it => it.IsChecked = user.Roles.Any(p => p.Id == it.Id));

            userDTO.Roles = rolesDTO;
            userDTO.Groups = groupsDTO;
            return userDTO;
        }

        /// <summary>Register new user</summary>
        /// <param name="dto">Data transfer object</param>
        /// <returns>Empty on success, UsernameAlreadyExists if username exists</returns>
        [HttpPost]
        public HttpResponseMessage RegisterUser(RegisterUserDTO dto)
        {
            var user = this.mapper.Map<User>(dto);

            try
            {
                this.service.CreateUser(user, dto.Pass);
            }
            catch (ValidationException)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { UsernameAlreadyExists = true });
            }

            this.SendNotification(user, dto.Language);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>Get user for ticket</summary>
        /// <param name="id">encoded ticket</param>
        /// <returns>user dto</returns>
        [HttpGet]
        public UserDTO GetUserByTicket(string id)
        {
            var ticket = this.securityService.DecodeTicket(id);

            Guid ticketGuid;
            if (!Guid.TryParse(ticket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            var user = this.securityService.GetUserByTicket(ticket);
            return user != null ? this.mapper.Map<UserDTO>(user) : null;
        }

        /// <summary>Reset ticket and change user password</summary>
        /// <param name="dto">ticket with new userpassword</param>
        /// <returns>return true if success</returns>                
        [HttpPost]
        public bool ResetRegisterTicket(ResetRegisterTicketDTO dto)
        {
            bool result = false;
            var decodedTicket = this.securityService.DecodeTicket(dto.Ticket);

            Guid ticketGuid;
            if (!Guid.TryParse(decodedTicket, out ticketGuid))
            {
                throw new ArgumentException("Ticket is not a valid Guid", "ticket");
            }

            var user = this.securityService.GetUserByTicket(decodedTicket);
            if (user != null)
            {
                //// reset ticket
                this.securityService.MarkUserTicketAsUsed(decodedTicket);

                //// change password
                var token = WebSecurity.GeneratePasswordResetToken(user.Name);
                result = WebSecurity.ResetPassword(token, dto.User.Password);
            }
            else
            {
                throw new ArgumentException("Ticket is not found for this user", "ticket");
            }

            return result;
        }

        /// <summary>Save user</summary>
        /// <param name="dto">User DTO</param>
        /// <returns>Empty on success, UsernameAlreadyExists if user is created and username exists</returns>
        [HttpPost]
        [HasPermission(Permissions = "ManageUsers")]
        public HttpResponseMessage SaveUser(UserDTO dto)
        {
            var permissions = this.service.GetEffectivePermissions().Select(p => p.Code);

            // create user
            if (dto.Id == 0)
            {
                var user = this.mapper.Map<User>(dto);
                user.Groups = new List<Group>();
                user.Roles = new List<Role>();

                if (permissions.Contains("ManageGroups"))
                {
                    this.UpdateGroupsCollection(user.Groups, dto.Groups);
                }

                if (permissions.Any(p => new[] { "ManangeRoles", "AssignUsersToRoles" }.Contains(p)))
                {
                    this.UpdateRolesCollection(user.Roles, dto.Roles);
                }

                //// create temp password for user
                if (dto.IsEmailUserToRegister)
                {
                    dto.Password = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(10);
                }

                try
                {
                    this.service.CreateUser(user, dto.Password);
                }
                catch (ValidationException)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { UsernameAlreadyExists = true });
                }

                //// send notification with userlink
                if (dto.IsEmailUserToRegister)
                {
                    var url = "userregistration".CreateFullUrl(Request);

                    var ticket = this.securityService.CreateTicketForUser(user);
                    if (ticket != null)
                    {
                        var link = this.securityService.EncodeTicket(url, new Guid(ticket));
                        this.SendNotification(user, dto.Language, link);
                    }
                }
            }
            else
            {
                //// update user                
                this.dataService.Update<User>(
                    dto.Id,
                    user =>
                    {
                        this.mapper.Map(dto, user);
                        if (permissions.Contains("ManageGroups"))
                        {
                            this.UpdateGroupsCollection(user.Groups, dto.Groups);
                        }

                        if (permissions.Any(p => new[] { "ManangeRoles", "AssignUsersToRoles" }.Contains(p)))
                        {
                            this.UpdateRolesCollection(user.Roles, dto.Roles);
                        }
                    });

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    var token = WebSecurity.GeneratePasswordResetToken(dto.Name);
                    WebSecurity.ResetPassword(token, dto.Password);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="dto">User DTO</param>
        [HttpPost]
        [Authorize]
        public void UpdateUser(UpdateUserDTO dto)
        {
            this.dataService.Update<User>(dto.Id, user => this.mapper.Map(dto, user));
        }

        /// <summary>
        /// Assign users to role.
        /// </summary>
        /// <param name="dto">Assign Users to Role DTO</param>
        [HasPermission(Permissions = "ManageUsers, AssignUsersToRoles")]
        public void AssignUsersToRole(AssignUsersToRoleDTO dto)
        {
            var role = this.dataService.Find<Role>(dto.RoleId);

            foreach (int userId in dto.UserIds)
            {
                if (this.dataService.Find<User>(userId).Roles.Any(r => r.Id == dto.RoleId))
                {
                    continue;
                }
                
                this.dataService.Update<User>(userId, user => user.Roles.Add(role));
            }
        }

        /// <summary>
        /// Assign users to group.
        /// </summary>
        /// <param name="dto">Assign Users to Group DTO</param>
        [HasPermission(Permissions = "ManageUsers")]
        public void AssignUsersToGroup(AssignUsersToGroupDTO dto)
        {
            var group = this.dataService.Find<Group>(dto.GroupId);

            foreach (int userId in dto.UserIds)
            {
                if (this.dataService.Find<User>(userId).Groups.Any(r => r.Id == dto.GroupId))
                {
                    continue;
                }

                this.dataService.Update<User>(userId, user => user.Groups.Add(group));
            }
        }

        /// <summary>
        /// Get current loggined user id
        /// </summary>
        /// <returns>Current user id</returns>
        [HttpGet]
        [Authorize]
        public int GetCurrentUserId()
        {
            return this.service.GetCurrentUserId();
        }

        /// <summary>
        /// The change password
        /// </summary>
        /// <param name="dto">The change password DTO</param>
        [HttpPost]
        [Authorize]
        public void ChangePassword(ChangePasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword) || string.IsNullOrEmpty(dto.NewPassword))
            {
                throw new InvalidOperationException("Current password or New password is null");
            }

            bool success = WebSecurity.ChangePassword(dto.Name, dto.CurrentPassword, dto.NewPassword);
            if (!success)
            {
                throw new InvalidOperationException("Current password is wrong. Please try again!");
            }
        }

        /// <summary>
        /// Recover password
        /// </summary>
        /// <param name="dto">The change password DTO</param>
        /// <returns>Recover password result DTO</returns>
        [HttpPost]
        public RecoverPasswordResultDTO RecoverPassword(RecoverPasswordDTO dto)
        {
            var result = new RecoverPasswordResultDTO();
            try
            {
                this.SendPasswordResetImpl(dto);
            }
            catch (Exception exc)
            {
                result.Exception = exc.Message;
                return result;
            }

            result.Successfully = true;
            return result;
        }        

        /// <summary>
        /// Send password resed
        /// </summary>
        /// <param name="dto">Recover password DTO</param>
        [HttpPost]
        [HasPermission(Permissions = "ManageUsers")]
        public void SendPasswordResetByAdmin(RecoverPasswordDTO dto)
        {
            this.SendPasswordResetImpl(dto, "ResetPasswordByAdmin");
        }

        [HttpGet]
        [Authorize]
        public bool NeedClearLocalDb()
        {
            return this.service.GetCurrentUser().ClearLocalDb ?? false;
        }

        private void SendPasswordResetImpl(RecoverPasswordDTO dto, string templateCode = "ResetPassword")
        {
            var user = this.service.GetUserByName(dto.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User with such email isn't registered");
            }
            //// Language should be the one preferred by user
            var language = (user.UserSettings == null || user.UserSettings.Language == null) ? ConfigurationManager.AppSettings[ConfigFallbackLanguage] 
                : user.UserSettings.Language.Id;
            var ticket = this.securityService.CreateTicketForUser(user);
            if (ticket != null)
            {
                var url = "resetpassword".CreateFullUrl(Request);
                var link = this.securityService.EncodeTicket(url, new Guid(ticket));
                this.SendResetPasswordNotification(user, language, link, templateCode);
            }
        }

        private void UpdateGroupsCollection(ICollection<Group> groups, IList<CheckBoxItemDTO> dtos)
        {
            if (groups == null || dtos == null)
            {
                return;
            }

            foreach (var group in dtos)
            {
                var item = groups.FirstOrDefault(p => p.Id == group.Id);

                if (group.IsChecked)
                {
                    if (item == null)
                    {
                        groups.Add(this.dataService.Find<Group>(group.Id));
                    }
                }
                else
                {
                    if (item != null)
                    {
                        groups.Remove(item);
                    }
                }
            }
        }

        private void UpdateRolesCollection(ICollection<Role> roles, IList<CheckBoxItemDTO> dtos)
        {
            if (roles == null || dtos == null)
            {
                return;
            }

            foreach (var role in dtos)
            {
                var item = roles.FirstOrDefault(p => p.Id == role.Id);

                if (role.IsChecked)
                {
                    if (item != null)
                    {
                        continue;
                    }

                    var r = this.dataService.Find<Role>(role.Id);
                    if (r.IsAdmin)
                    {
                        continue;
                    }

                    roles.Add(r);
                }
                else
                {
                    if (item == null || item.IsAdmin)
                    {
                        continue;
                    }

                    roles.Remove(item);
                }
            }
        }

        private void SendNotification(User user, string language, string userLink = "")
        {
            var tagValues = new NameValueCollection
                                {
                                    { "$UserName", string.Format("{0} {1}", user.FirstName, user.Surname) },
                                    { "$UserFirstName", user.FirstName },
                                    { "$UserSurname", user.Surname },
                                    { "$UserEmail", user.Name },
                                    { "$UserLink", userLink }
                                };

            var address = new MailAddress(user.Name, string.Format("{0} {1}", user.FirstName, user.Surname));
            this.sender.SendEmail("UserCreated", tagValues, language, new MailAddressCollection { address }, null);
        }

        private void SendResetPasswordNotification(User user, string language, string userLink, string templateCode)
        {
            var tagValues = new NameValueCollection
                                {
                                    { "$UserName", string.Format("{0} {1}", user.FirstName, user.Surname) },
                                    { "$UserFirstName", user.FirstName },
                                    { "$UserSurname", user.Surname },
                                    { "$UserEmail", user.Name },
                                    { "$UserLink", userLink }
                                };

            var address = new MailAddress(user.Name, string.Format("{0} {1}", user.FirstName, user.Surname));
            this.sender.SendEmail(templateCode, tagValues, language, new MailAddressCollection { address }, null);
        }
    }
}