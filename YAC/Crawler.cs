using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private ConcurrentQueue<Uri> _queue;
        private ConcurrentBag<Uri> _crawled;
        private ConcurrentBag<string> _results;
        private IEnumerable<string> _disallowedUrls;

        public Crawler(IWebAgent webAgent)
        {
            _webAgent = webAgent;
        }

        private void Setup(Uri domain)
        {
            // add the domain to the queue first
            _queue = new ConcurrentQueue<Uri>(new List<Uri> {domain});
            _crawled = new ConcurrentBag<Uri>();
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

            if (threads.Count() > 1)
            {
                // once work comes in for each thread, release from the holding pattern and allow them to work
                for (int i = 1; i < threads.Count(); i++)
                {
                    if (_queue.Count >= i)
                    {
                        threads[i].Start();
                    }
                }
            }

            // flush queues and return the list of data found during the crawl
            foreach (var thread in threads)
            {
                if(thread.ThreadState == ThreadState.Running)
                    thread.Join();
            }

            return _results;
        }

        private List<Thread> CreateThreads(CrawlJob job)
        {
            var threads = new List<Thread>();

            for (int i = 0; i < job.ThreadAllowance; i++)
            {
                threads.Add(
                    new Thread(
                        async () => await ThreadAction(
                            job.CompletionConditions, 
                            job.CancellationToken)));
            }

            return threads;
        }

        private async Task ThreadAction(IEnumerable<ICrawlCompletionCondition> completionConditions, CancellationToken cancellationToken)
        {
            while (completionConditions.All(cc => !cc.ConditionMet()) && !cancellationToken.IsCancellationRequested &&
                   !_aThreadIsComplete)
            {
                // get the next Uri to crawl
                var next = GetNext();

                // access it
                var response = _webAgent.ExecuteRequest(next);

                // log that we've crawled it
                _crawled.Add(next);

                // access the contents
                using (var reader = new StreamReader(_webAgent.GetCompressedStream(await response)))
                {
                    // read out contents
                    var html = reader.ReadToEnd();

                    // parse the contents for new links and data user wants

                    // add links found to queue if they're part of the domain and not already crawled and not already in the queue
                    // and not a disallowed url
                    // and make sure the queue is not too big

                    // add data matching the regex to the return list
                }
            }

            if (!_aThreadIsComplete)
                _aThreadIsComplete = true;
        }

        private Uri GetNext()
        {
            if (_queue.IsEmpty)
                return null;

            var result = _queue.TryDequeue(out Uri next);
            if (!result)
                next = GetNext();

            return next;
        }

        public void Dispose()
        {

        }
    }
}
