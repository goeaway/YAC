using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YAC.Abstractions
{
    public interface ICrawler : IDisposable
    {
        Task<IEnumerable<string>> Crawl(CrawlJob job);
    }
}
