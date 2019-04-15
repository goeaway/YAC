using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YAC.Abstractions
{
    public interface IWebAgent
    {
        string AgentName { get; }
        /// <summary>
        /// Gets a collection of cookies which are added to each request
        /// </summary>
        IList<Cookie> Cookies { get; set; }
        Task<HttpWebResponse> ExecuteRequest(Uri uri);
        Stream GetCompressedStream(HttpWebResponse response);
    }
}
