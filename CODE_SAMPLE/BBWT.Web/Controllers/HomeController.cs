namespace BBWT.Web.Controllers
{
    using BBWT.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using BBWT.Web.Attributes;

    /// <summary>
    /// Home page controller
    /// </summary>
    [WinLogin]
    public class HomeController : Controller
    {
        private readonly IMembershipService service;
        private readonly IPhantomService phantomService;

        private string output = string.Empty;

        /// <summary>
        /// Creates HomeController instance
        /// </summary>
        /// <param name="svc">Membership service instance</param>
        /// <param name="phantomService">The PhantomJS service instance</param>
        public HomeController(IMembershipService svc, IPhantomService phantomService)
        {
            this.service = svc;
            this.phantomService = phantomService;
        }

        /// <summary>
        /// Generates home page html
        /// </summary>
        /// <returns>Home page view</returns>
        //// [OutputCache(Location = System.Web.UI.OutputCacheLocation.Any, Duration = 300)]
        public ActionResult Index()
        {
            var browser = Request.Browser;
            if (browser.Browser == "IE" && browser.MajorVersion < 10)
            {
                return this.View("NotCompatible");
            }

            var user = this.service.GetCurrentUser();
            this.ViewBag.currentUser = 
                user == null ? 
                "null" :
                string.Format("{{'Id':{0}, 'Name':'{1}', 'FullName':'{2}', 'FirstName':'{3}', 'Surname':'{4}'}}", user.Id, user.Name, user.FirstName + " " + user.Surname, user.FirstName, user.Surname);

            if (user != null)
            {
                this.ViewBag.currentPermissions = this.service.GetEffectivePermissions()
                          .Select(p => p.Code)
                          .Union(new List<string> { "authorized" });
            }
            else
            {
                this.ViewBag.currentPermissions = string.Empty;
            }

            string escapedFragment = this.Request.QueryString["_escaped_fragment_"];
            if (!string.IsNullOrEmpty(escapedFragment))
            {
                ContentResult result = new ContentResult();
                result.ContentType = "text/html";

                result.Content = (string)this.HttpContext.Cache.Get(this.Request.Url.OriginalString);
                if (string.IsNullOrEmpty(result.Content))
                {
                    result.Content = this.phantomService.GetWebPage(this.Request.Url, escapedFragment, this.Request.PhysicalApplicationPath);
                    this.HttpContext.Cache.Add(this.Request.Url.OriginalString, result.Content, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromDays(1), System.Web.Caching.CacheItemPriority.Low, null);
                }
                
                return result;
            }

            return this.View();
        }

        /// <summary>
        /// Login Cap
        /// </summary>
        /// <returns>Result</returns>
        public bool LoginCap()
        {
            return true;
        }
    }
}