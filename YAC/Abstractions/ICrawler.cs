using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YAC.Models;

namespace YAC.Abstractions
{
    public interface ICrawler : IDisposable
    {
        bool IsRunning { get; }
        void Cancel();
        Task<CrawlReport> Crawl(CrawlJob job);
    }
}
