using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;

namespace YAC.Web
{
    public class RollingWindowRateLimiter : IRateLimiter
    {
        public bool CanAccess()
        {
            throw new NotImplementedException();
        }

        public void HoldUntilReady()
        {
            throw new NotImplementedException();
        }
    }
}
