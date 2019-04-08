using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YAC.Abstractions;

namespace YAC.Web
{
    public class WebAgent : IWebAgent
    {
        private readonly IRateLimiter _rateLimiter;

        public WebAgent(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        public async Task<HttpWebResponse> ExecuteRequest(Uri uri)
        {
            // create request object


            // make sure the rate limiter is happy

            // make request when ready

            throw new NotImplementedException();
        }
    }
}
