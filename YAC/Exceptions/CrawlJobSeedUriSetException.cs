using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Exceptions
{
    public class CrawlJobSeedUriSetException : YACException
    {
        public CrawlJobSeedUriSetException() : base("Seed URI must start with the Domain") { }
    }
}
