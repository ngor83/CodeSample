namespace BBWT.Services.Classes
{
    using System;
    using System.Net;
    using System.Web;
    using System.Web.Caching;

    /// <summary>
    /// Keep alive service
    /// </summary>
    public class KeepAlive
    {
        private static KeepAlive instance;

        private static object sync = new object();

        private string applicationUrl;

        private string cacheKey;

        private KeepAlive(string applicationUrl)
        {
            this.applicationUrl = applicationUrl;
            this.cacheKey = Guid.NewGuid().ToString();
            instance = this;
        }

        /// <summary>
        /// Shows if service is working
        /// </summary>
        public static bool IsKeepingAlive
        {
            get
            {
                lock (sync)
                {
                    return instance != null;
                }
            }
        }

        /// <summary>
        /// Start service
        /// </summary>
        /// <param name="applicationUrl">Url to ping</param>
        public static void Start(string applicationUrl)
        {
            if (IsKeepingAlive)
            {
                return;
            }

            lock (sync)
            {
                instance = new KeepAlive(applicationUrl);
                instance.Insert();
            }
        }

        /// <summary>
        /// Stop service
        /// </summary>
        public static void Stop()
        {
            lock (sync)
            {
                HttpRuntime.Cache.Remove(instance.cacheKey);
                instance = null;
            }
        }

        private void Callback(string key, object value, CacheItemRemovedReason reason)
        {
            if (reason == CacheItemRemovedReason.Expired)
            {
                this.FetchApplicationUrl();
                this.Insert();
            }
        }

        private void FetchApplicationUrl()
        {
            try
            {
                var request = WebRequest.Create(this.applicationUrl) as HttpWebRequest;
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    var status = response.StatusCode;

                    ////log status
                }
            }
            catch
            {
                ////log exception
            }
        }

        private void Insert()
        {
            HttpRuntime.Cache.Add(
                this.cacheKey, 
                this, 
                null, 
                Cache.NoAbsoluteExpiration, 
                new TimeSpan(0, 10, 0), 
                CacheItemPriority.Normal, 
                this.Callback);
        }
    }
}