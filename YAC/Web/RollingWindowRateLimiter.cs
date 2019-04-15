using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using YAC.Abstractions;

namespace YAC.Web
{
    public class RollingWindowRateLimiter : IRateLimiter
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _timeStamps
            = new ConcurrentDictionary<string, List<DateTime>>();

        private readonly int _maxAccessesPerPeriod;
        private readonly TimeSpan _window;

        public RollingWindowRateLimiter(int maxAccessesPerPeriod, TimeSpan window)
        {
            _maxAccessesPerPeriod = maxAccessesPerPeriod;
            _window = window;
        }

        public void HoldUntilReady(string domain)
        {
            if(domain == null)
                throw new ArgumentNullException(nameof(domain));

            // need to check if this domain is allowed to be accessed
            // the timestamps dict will hold an entry for each domain and with that a list of the most recent accesses
            // first check the dict for this domain as the key, 
            var exists = _timeStamps.TryGetValue(domain, out var times);

            // if nothing, add the domain as a key and add a single item using DateTime.Now to it, then return
            if (!exists)
            {
                _timeStamps.TryAdd(domain, new List<DateTime> { DateTime.Now });
                return;
            }

            // recursively checks and removes stale timestamps until a new request can be made
            RecursiveHold(times);
        }

        private void RecursiveHold(List<DateTime> times)
        {
            // if there is something for this domain, check that the amount of times in it's collection is <= the set amount of accesses
            // if it is less, add DateTime.Now to the collection and return
            if (times.Count <= _maxAccessesPerPeriod)
            {
                times.Add(DateTime.Now);
                return;
            }

            // if not, check each of the existing items in the collection, any DateTimes that are older than the set window bound should be removed
            times = times.Where(t => t >= (DateTime.Now - _window)).ToList();

            Thread.Sleep(10);

            RecursiveHold(times);
        }

        public bool CanAccess(string domain)
        {
            if(domain == null) 
                throw new ArgumentNullException(nameof(domain));

            var exists = _timeStamps.TryGetValue(domain, out var times);
            // if nothing there for this domain then we can access
            if (!exists)
            {
                return true;
            }
            // try return early if we can
            if (times.Count <= _maxAccessesPerPeriod)
            {
                return true;
            }
            // clear out any stale times 
            times = times.Where(t => t >= (DateTime.Now - _window)).ToList();
            // check again
            return times.Count <= _maxAccessesPerPeriod;
        }
    }
}
