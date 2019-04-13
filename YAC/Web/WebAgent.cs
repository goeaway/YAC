using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YAC.Abstractions;
using YAC.Abstractions.Services;

namespace YAC.Web
{
    public class WebAgent : IWebAgent
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly IProxyService _proxyService;
        private readonly Dictionary<string, Func<Stream, Stream>> _acceptedEncoding;

        public string AgentName => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";

        private const int MAX_RESPONSE_HEADER_LENGTH = 200;
        private const int REQUEST_TIMEOUT = 3000;
        private const int DEFAULT_CONNECTION_LIMIT = 100;

        private string EncodingString
            => string.Join(",", _acceptedEncoding.Select(e => e.Key));

        public WebAgent(IRateLimiter rateLimiter, IProxyService proxyService)
        {
            _rateLimiter = rateLimiter;
            _proxyService = proxyService;
            _acceptedEncoding = new Dictionary<string, Func<Stream, Stream>>
            {
                { "gzip", (stream) => new GZipStream(stream, CompressionMode.Decompress)  },
                { "deflate", (stream) => new DeflateStream(stream, CompressionMode.Decompress) }
            };
        }

        public async Task<HttpWebResponse> ExecuteRequest(Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);

            request.UserAgent = AgentName;
            request.Timeout = REQUEST_TIMEOUT;

            request.Headers.Add("Accept-Encoding", EncodingString);

            request.KeepAlive = true;
            request.MaximumResponseHeadersLength = MAX_RESPONSE_HEADER_LENGTH;

            request.Proxy = _proxyService.GetProxy();

            ServicePointManager.DefaultConnectionLimit = DEFAULT_CONNECTION_LIMIT;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            
            _rateLimiter.HoldUntilReady(uri.Host);
            return (HttpWebResponse)(await request.GetResponseAsync());
        }

        public Stream GetCompressedStream(HttpWebResponse response)
        {
            if (response.ContentEncoding != null)
            {
                // check each accepted and return a new stream if its contained
                foreach (var encoding in _acceptedEncoding)
                {

                    // Value is the initialiser method
                    if (response.ContentEncoding.ToLower().Contains(encoding.Key))
                        return encoding.Value(response.GetResponseStream());
                }
            }

            // give back the original if no encoders were found
            return response.GetResponseStream();
        }
    }
}
