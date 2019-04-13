using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace YAC.Abstractions.Services
{
    public interface IProxyService
    {
        IWebProxy GetProxy();
    }
}
