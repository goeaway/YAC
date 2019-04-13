using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
                    Domain = new Uri("https://reddit.com/r/pics"),
                    CompletionConditions = new List<ICrawlCompletionCondition>
                    {
                        new MaxPagesCrawledCondition(100),
                        new MaxTimeCondition(TimeSpan.FromMinutes(3)),
                        new MaxResultsFoundCondition(2000)
                    },
                    ThreadAllowance = 10,
                    Regex = "<img.+?src\"(?<image>.+?)\""
                };
                var results = await crawler.Crawl(job);

                Console.WriteLine(results.CrawlCount);
                Console.WriteLine(results.QueueSize);
                Console.WriteLine(results.ResultsCount);
            }
        }
    }
}
