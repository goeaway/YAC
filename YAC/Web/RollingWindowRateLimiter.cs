using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;

namespace YAC.Web
{
    public class RollingWindowRateLimiter : IRateLimiter
    {
        public bool CanAccess(string domain)
        {
            throw new NotImplementedException();
        }

        public void HoldUntilReady(string domain)
        {
            throw new NotImplementedException();
        }
    }
}
