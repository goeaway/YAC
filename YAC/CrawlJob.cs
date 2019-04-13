using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using YAC.Abstractions;
using YAC.Exceptions;

namespace YAC
{
    public class CrawlJob
    {
        public Uri Domain { get; set; }

        private IReadOnlyCollection<Uri> _seedUris = new List<Uri>();
        public IReadOnlyCollection<Uri> SeedUris
        {
            get => _seedUris.Count > 0 ? _seedUris : new List<Uri> {Domain};
            set
            {
                // if any seed does not start with the domain we throw
                if(_seedUris.Any(su => !su.ToString().StartsWith(Domain.ToString())))
                    throw new CrawlJobSeedUriSetException();

                _seedUris = value;
            }
        }

        public string Regex { get; set; }
        public IEnumerable<ICrawlCompletionCondition> CompletionConditions { get; set; }
        public int ThreadAllowance { get; set; }
    }
}
