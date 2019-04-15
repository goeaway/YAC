using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using YAC.Abstractions;
using YAC.Exceptions;

namespace YAC
{
    public class CrawlJob
    {
        /// <summary>
        /// Gets or sets a <see cref="Uri"/> object which represents the domain the crawler is scraping. The crawler will not access a page outside of this domain.
        /// You can limit the crawler to a specific area of a domain or allow the crawler anywhere on a domain. 
        /// </summary>
        public Uri Domain { get; set; }
        private IReadOnlyCollection<Uri> _seedUris = new List<Uri>();
        /// <summary>
        /// Gets or sets a collection of <see cref="Uri"/> objects the crawler should scrape first. Each of these must start with the Domain.
        /// </summary>
        public IReadOnlyCollection<Uri> SeedUris
        {
            get => _seedUris.Count > 0 ? _seedUris : new List<Uri> {Domain};
            set
            {
                // if any seed does not start with the domain we throw
                if(_seedUris.Any(su => !su.ToString().StartsWith(Domain.ToString())))
                    throw new CrawlJobSeedUriSetException();

                _seedUris = value;
            }
        }
        /// <summary>
        /// Gets or sets a custom regex pattern the crawler will use when scraping for data. Links will automatically be collected if
        /// the FindNewLinks property is set to true.
        /// </summary>
        public string Regex { get; set; }
        /// <summary>
        /// Gets or sets a collection of <see cref="ICrawlCompletionCondition"/> which is used by the crawler to decide if the crawl is complete.
        /// </summary>
        public IEnumerable<ICrawlCompletionCondition> CompletionConditions { get; set; } = new List<ICrawlCompletionCondition>();
        /// <summary>
        /// Gets or sets a collection of <see cref="ICrawlEnqueueCondition"/> which is by the crawler to decide if a URI should be enqueued
        /// </summary>
        public IEnumerable<ICrawlEnqueueCondition> EnqueueConditions { get; set; } = new List<ICrawlEnqueueCondition>();
        /// <summary>
        /// Gets or sets the number of threads the crawler is allowed to spawn to crawl. Minimum is 1
        /// </summary>
        public int ThreadAllowance { get; set; }
        /// <summary>
        /// Gets or sets if the crawler should find new links when scraping pages. Defaults to true, you may want to set this to false if
        /// you only want to crawl the pages in the SeedUris collection
        /// </summary>
        public bool FindNewLinks { get; set; } = true;
        /// <summary>
        /// Gets or sets a collection of <see cref="Cookie" /> to be added to each request for this crawl
        /// </summary>
        public IList<Cookie> Cookies { get; set; } = new List<Cookie>();
    }
}
