using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Models
{
    public class CrawlProgress
    {
        public DateTime Start { get; set; }
        public TimeSpan CrawlDuration => DateTime.Now - Start;
        public int QueueSize { get; set; }
        public int CrawlCount { get; set; }
        public int ResultsCount { get; set; }
        public List<Exception> Exceptions { get; set; }
    }
}
