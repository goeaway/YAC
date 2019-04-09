using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YAC.Abstractions;

namespace YAC.Web
{
    public static class RobotParser
    {
        public static async Task<IEnumerable<string>> GetDisallowedUrls(IWebAgent webAgent, string domain)
        {
            var uri = new Uri($"http://{domain}/robots.txt");
            var list = new List<string>();
            var text = "";
            using (var response = await webAgent.ExecuteRequest(uri))
            {
                if ((int) response.StatusCode >= 400 || (int) response.StatusCode <= 599)
                {
                    return list;
                }

                using (var stream = webAgent.GetCompressedStream(response))
                using (var reader = new StreamReader(stream, Encoding.Default))
                {
                    text = reader.ReadToEnd();
                }
            }

            var lines = text.ToLower().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var name = webAgent.AgentName.ToLower();
            var applicable = false;

            foreach (var line in lines)
            {
                if (line.Contains("user-agent"))
                {
                    applicable = line.Contains("*") || line.Contains(name);
                }

                if (line.Contains("disallow") && applicable)
                {
                    var split = line.Split(':');
                    if (split.Length < 2)
                    {
                        continue;
                    }

                    var rule = split[1].Trim();
                    
                    list.Add(rule);
                }
            }

            return list;
        }

        public static bool UriIsAllowed(IEnumerable<string> disallowed, Uri uri)
        {
            foreach (var url in disallowed)
            {
                var uriStr = uri.ToString();
                if (url.Length > uriStr.Length)
                {
                    continue;
                }

                var sub = uriStr.Substring(0, url.Length);

                // return early if we got one
                if (sub == url)
                    return false;
            }

            return true;
        }
    }
}
