using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YAC.Abstractions
{
    public interface IWebAgent
    {
        Task<HttpWebResponse> ExecuteRequest(Uri uri);
    }
}
