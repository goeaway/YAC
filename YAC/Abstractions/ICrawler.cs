using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YAC.Models;

namespace YAC.Abstractions
{
    public interface ICrawler : IDisposable
    {
        Task<CrawlReport> Crawl(CrawlJob job);
    }
}
