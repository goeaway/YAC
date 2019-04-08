using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using YAC.Abstractions;

namespace YAC
{
    public struct CrawlJob
    {
        public Uri Domain { get; set; }
        public string Regex { get; set; }
        public IEnumerable<ICrawlCompletionCondition> CompletionConditions { get; set; }
        public int ThreadAllowance { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
