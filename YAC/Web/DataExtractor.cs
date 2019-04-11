using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using YAC.Models;

namespace YAC.Web
{
    public static class DataExtractor
    {
        private const string LINK_REGEX = "<a.+?href=\"(?<link>.+?)\".+?>";

        public static ExtractedData Extract(string html, Uri domain, string pattern)
        {
            var primedPattern = string.IsNullOrEmpty(pattern) ? "" : "|" + pattern;
            var combinedRegexPatterns = LINK_REGEX + primedPattern;

            var regex = new Regex(combinedRegexPatterns);

            var matches = regex.Matches(html);
            
            var data = new ExtractedData();

            foreach(Match m in matches)
            {
                if (m.Success)
                {
                    var link = m.Groups["link"];
                    var value = link.Value;

                    // false links go here
                    if (value.Contains("#") || value.Contains("javascript:void(0)"))
                        continue;

                    // full URLs sometimes hide behind "//"
                    if (value.StartsWith("//"))
                        value = value.Substring(2);

                    // add the link if:
                    // it starts with the whole domain
                    // it starts with the domain (without the host)
                    // it starts with a single / and the domain is only the host (no extra path)
                    if (value.StartsWith(domain.OriginalString))
                    {
                        data.Links.Add(new Uri(value));
                    }
                    else if (value.StartsWith(domain.AbsolutePath))
                    {
                        data.Links.Add(new Uri("http://" + domain.Host + value));
                    }
                }
            }

            return data;
        } 
    }
}
