namespace BBWT.Web.WebAPI
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web;
    using System.Web.Http;
    using System.Web.Security;
    
    using AutoMapper;

    using BBWT.Data.Membership;
    using BBWT.DTO.Membership;
    using BBWT.Services.Interfaces;
    using BBWT.Web.Helpers;

    using WebMatrix.WebData;
    using System;

    /// <summary>
    /// Controller to support auth process
    /// </summary>
    public class AuthController : ApiController
    {
        private readonly IMembershipService membershipService;

        private readonly IMappingEngine mapper;

        private readonly ICommonDataService dataService;

        private readonly IDomainService domainservice;

        /// <summary>Creates Auth controller</summary>
        /// <param name="membershipService">Membership Service injection</param>
        /// <param name="mapper">Mapper injection</param>
        /// <param name="dataService">Common Data Service</param>
        /// <param name="domainservice">Domain service</param>
        public AuthController(IMembershipService membershipService, IMappingEngine mapper, ICommonDataService dataService,  IDomainService domainservice)
        {
            this.membershipService = membershipService;
            this.mapper = mapper;
            this.dataService = dataService;
            this.domainservice = domainservice;
        }

        /// <summary>
        /// Get current user if autentificated
        /// </summary>
        /// <returns>Current user if any</returns>
        [HttpGet]
        public AccountDTO CurrentUser()
        {            
            if (WebSecurity.HasUserId)
            {
                return this.mapper.Map<AccountDTO>(this.dataService.Find<User>(WebSecurity.CurrentUserId));                
            }

            return null;
        }


        /// <summary>Login user</summary>
        /// <param name="data">credentials</param>
        /// <returns>Found user</returns>
        [HttpPost][HttpGet]
        public HttpResponseMessage Login([FromBody] LoginDTO data)
        {
            HttpResponseMessage response = null;
            var domain = this.domainservice.GetCurrentDomainAlias(HttpContext.Current.Request.Url.Host);
            var rolename = domain == "kis" ? "KIS user" : "TS user";

            var user = this.membershipService.GetUserByName(data.User);
            //// check if user exists (need separate message for client if doesn't)
            if (user != null)
            {
                //// login
                if (Membership.ValidateUser(data.User, data.Pass))
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, this.mapper.Map<AccountDTO>(user));
                    response.Headers.AddCookies(new CookieHeaderValue[]
                    {
                        user.CreateCookieHeader(false)
                    });
                    return response;
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, new { InvalidPassword = true });
                }
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.OK, new { UserNotFound = true });
            }

            return response;
        }

        /// <summary>
        /// Logout current user if logged in
        /// </summary>
        /// <returns>boolean result</returns>
        [HttpPost]
        [Authorize]
        public bool Logout()
        {
            if (WebSecurity.HasUserId)
            {
                WebSecurity.Logout();
                return true;
            }
            
            return false;
        }        

        /// <summary>
        /// Get list of effective permissions for current user
        /// </summary>
        /// <returns>list of effective permissions</returns>
        [HttpPost]
        [Authorize]
        public List<string> Permissions()
        {                        
            var res = this.membershipService.GetEffectivePermissions()
                .Select(p => p.Code)
                .ToList();

            res.Add("authorized");
            return res;
        }
    }
}