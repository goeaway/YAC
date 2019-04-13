using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using YAC.Models;

namespace YAC.Web
{
    public static class DataExtractor
    {
        private const string LINK_REGEX = "<a.+?href=\"(?<yaclink>.+?)\".+?>";

        public static ExtractedData Extract(string html, Uri domain, string pattern)
        {
            var customRegexUsed = !string.IsNullOrEmpty(pattern);
            var primedPattern = customRegexUsed ? "|" + pattern : "";
            var combinedRegexPatterns = LINK_REGEX + primedPattern;

            var regex = new Regex(combinedRegexPatterns);

            var matches = regex.Matches(html);
            
            var data = new ExtractedData();

            foreach(Match m in matches)
            {
                if (m.Success)
                {
                    var link = m.Groups["yaclink"];

                    if (link != null)
                    {
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
                        if (value.StartsWith(domain.OriginalString))
                        {
                            data.Links.Add(new Uri(value));
                        }
                        else if (value.StartsWith(domain.AbsolutePath))
                        {
                            data.Links.Add(new Uri($"{domain.Scheme}://" + domain.Host + value));
                        }
                    }

                    // try to add data if the user has set a regex
                    if (customRegexUsed)
                    {
                        foreach (var groupName in regex.GetGroupNames())
                        {
                            // don't add the yaclinks to the data and group 0 is the entire string (expect the custom regex has a capture group)
                            if (groupName == "yaclink" || groupName == "0")
                                continue;

                            var value = m.Groups[groupName];

                            if (value.Success)
                            {
                                data.Data.Add(new Tuple<string, string>(groupName, value.Value));
                            }
                        }
                    }
                }
            }

            return data;
        } 
    }
}
