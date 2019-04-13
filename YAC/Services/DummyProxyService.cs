using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using YAC.Abstractions.Services;

namespace YAC.Services
{
    public class DummyProxyService : IProxyService
    {
        public IWebProxy GetProxy()
        {
            return WebRequest.DefaultWebProxy;
        }
    }
}
