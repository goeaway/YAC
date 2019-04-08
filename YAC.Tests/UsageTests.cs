using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAC.Abstractions;
using YAC.CrawlCompletionConditions;
using YAC.Web;

namespace YAC.Tests
{
    [TestClass]
    public class UsageTests
    {
        [TestMethod]
        public async Task Test()
        {
            var limiter = new RollingWindowRateLimiter();
            var agent = new WebAgent(limiter);
            using (var crawler = new Crawler(agent))
            {
                var job = new CrawlJob
                {
                    Domain = new Uri("https://google.com"),
                    CompletionConditions = new List<ICrawlCompletionCondition>
                    {
                        new MaxPagesCrawledCondition(),
                        new MaxTimeCondition(),
                        new MaxResultsFoundCondition()
                    },
                    Regex = "q"
                };
                var results = await crawler.Crawl(job);
            }
        }
    }
}
