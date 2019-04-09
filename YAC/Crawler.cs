using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YAC.Abstractions;
using YAC.Web;

namespace YAC
{
    public class Crawler : ICrawler
    {
        private readonly IWebAgent _webAgent;

        private bool _aThreadIsComplete;
        private ConcurrentQueue<string> _queue;
        private ConcurrentBag<string> _crawled;
        private ConcurrentBag<string> _results;
        private IEnumerable<string> _disallowedUrls;

        public Crawler(IWebAgent webAgent)
        {
            _webAgent = webAgent;
        }

        private void Setup(Uri domain)
        {
            // add the domain to the queue first
            _queue = new ConcurrentQueue<string>(new List<string> {domain.ToString()});
            _crawled = new ConcurrentBag<string>();
            _results = new ConcurrentBag<string>();
            _aThreadIsComplete = false;
        }

        public async Task<IEnumerable<string>> Crawl(CrawlJob job)
        {
            Setup(job.Domain);

            // try and parse the robots.txt file of the domain and add any disallowed links to a read only collection
            _disallowedUrls = await RobotParser.GetDisallowedUrls(_webAgent, job.Domain.Host);

            // quit early as we are not allowed to go on this domain
            if (_disallowedUrls.Contains("/"))
                return _results;

            // create the allowed amount of threads for the job
            var threads = CreateThreads(job);

            // hold all but one thread in a pattern until there is work for them
            // start the first thread off, with the job of parsing the domain page provided
            threads.First().Start();

            // once work comes in for each thread, release from the holding pattern and allow them to work


            // flush queues and return the list of data found during the crawl
            foreach (var thread in threads)
            {
                thread.Join();
            }

            return _results;
        }

        private IEnumerable<Thread> CreateThreads(CrawlJob job)
        {
            var threads = new List<Thread>();

            for (int i = 0; i < job.ThreadAllowance; i++)
            {
                threads.Add(
                    new Thread(
                        () => ThreadAction(
                            new ManualResetEvent(false),
                            job.CompletionConditions, 
                            job.CancellationToken)));
            }

            return threads;
        }

        private void ThreadAction(ManualResetEvent doneEvent, IEnumerable<ICrawlCompletionCondition> completionConditions, CancellationToken cancellationToken)
        {
            while (completionConditions.All(cc => !cc.ConditionMet()) && !cancellationToken.IsCancellationRequested &&
                   !_aThreadIsComplete)
            {

            }

            doneEvent.Set();
            if (!_aThreadIsComplete)
                _aThreadIsComplete = true;

            WaitHandle.WaitAll(new ManualResetEvent[] {doneEvent});
        }

        private string GetNext()
        {
            if (_queue.IsEmpty)
                return null;

            var result = _queue.TryDequeue(out string next);
            if (!result)
                next = GetNext();

            return next;
        }

        public void Dispose()
        {

        }
    }
}
