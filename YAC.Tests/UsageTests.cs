using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAC.Abstractions;
using YAC.CrawlCompletionConditions;
using YAC.Services;
using YAC.Web;

namespace YAC.Tests
{
    [TestClass]
    public class UsageTests
    {
        [TestMethod]
        public async Task Test()
        {
            var limiter = new RollingWindowRateLimiter(10000, TimeSpan.FromMinutes(1));
            var proxyService = new DefaultProxyService();
            var agent = new WebAgent(limiter, proxyService);

            agent.Cookies.Add(new Cookie("over18", "1", "/", "reddit.com"));

            using (var crawler = new Crawler(agent))
            {
                var job = new CrawlJob()
                {
                    Domain = new Uri("https://reddit.com/r/"),
                    CompletionConditions = new List<ICrawlCompletionCondition>
                    {
                        new MaxPagesCrawledCondition(100),
                        new MaxTimeCondition(TimeSpan.FromMinutes(3)),
                        new MaxResultsFoundCondition(2000)
                    },
                    ThreadAllowance = 10,
                    Regex = "<img.+?src=\"(?<image>.+?)\""
                };
                var crawlTask = crawler.Crawl(job);

                var results = await crawlTask;

                Console.WriteLine(results.CrawlCount);
                Console.WriteLine(results.QueueSize);
                Console.WriteLine(results.ResultsCount);

                foreach (var item in results.Data)
                {
                    Console.WriteLine(item.Item2);
                }
            }
        }
    }
}
