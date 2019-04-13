using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Models
{
    public class CrawlReport : CrawlProgress
    {
        public IEnumerable<Tuple<string,string>> Data { get; set; }
    }
}
