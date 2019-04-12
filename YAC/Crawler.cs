using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using YAC.Abstractions;
using YAC.Web;

namespace YAC
{
    public class Crawler : ICrawler
    {
        private const int QUEUE_MAX = 2000;

        private readonly IWebAgent _webAgent;

        private bool _aThreadIsComplete;
        private ConcurrentQueue<Uri> _queue;
        private ConcurrentBag<Uri> _crawled;
        private ConcurrentBag<Tuple<string, string>> _results;
        private IEnumerable<string> _disallowedUrls;

        public Crawler(IWebAgent webAgent)
        {
            _webAgent = webAgent;
        }

        private void Setup(IReadOnlyCollection<Uri> seedUris)
        {
            // add the seeds to the queue first
            _queue = new ConcurrentQueue<Uri>(seedUris);
            _crawled = new ConcurrentBag<Uri>();
            _results = new ConcurrentBag<Tuple<string, string>>();
            _aThreadIsComplete = false;
        }

        public async Task<IEnumerable<Tuple<string, string>>> Crawl(CrawlJob job)
        {
            Setup(job.SeedUris);

            // try and parse the robots.txt file of the domain and add any disallowed links to a read only collection
            _disallowedUrls = await RobotParser.GetDisallowedUrls(_webAgent, job.Domain.Host);

            // quit early as we are not allowed to go on this domain
            if (_disallowedUrls.Contains("/"))
                return _results;

            // create the allowed amount of threads for the job
            var threadsAndDoneEvents = CreateThreads(job);
            // hold all but one thread in a pattern until there is work for them
            // start the first thread off, with the job of parsing the domain page provided
            threadsAndDoneEvents.Item1.First().Start();

            if (threadsAndDoneEvents.Item1.Count() > 1)
            {
                // once work comes in for each thread, release from the holding pattern and allow them to work
                for (int i = 1; i < threadsAndDoneEvents.Item1.Count(); i++)
                {
                    if (_queue.Count >= i)
                    {
                        threadsAndDoneEvents.Item1[i].Start();
                    }
                }
            }

            // wait for done events
            WaitHandle.WaitAll(threadsAndDoneEvents.Item2.ToArray());

            // flush queues and return the list of data found during the crawl
            foreach (var thread in threadsAndDoneEvents.Item1)
            {
                if(thread.ThreadState == ThreadState.Running)
                    thread.Join();
            }

            return _results;
        }

        private Tuple<List<Thread>, List<ManualResetEvent>> CreateThreads(CrawlJob job)
        {
            var threads = new List<Thread>();
            var events = new List<ManualResetEvent>();

            for (int i = 0; i < Math.Max(1, job.ThreadAllowance); i++)
            {
                var doneEvent = new ManualResetEvent(false);
                events.Add(doneEvent);
                threads.Add(
                    new Thread(
                        async () => await ThreadAction(job, doneEvent)));
            }

            return new Tuple<List<Thread>, List<ManualResetEvent>>(threads, events);
        }

        private async Task ThreadAction(CrawlJob job, ManualResetEvent doneEvent)
        {
            while (job.CompletionConditions.All(cc => !cc.ConditionMet()) && 
                   !job.CancellationToken.IsCancellationRequested &&
                   !_aThreadIsComplete)
            {
                // get the next Uri to crawl
                var next = GetNext();

                if (next == null)
                {
                    break;
                }

                try
                {
                    // access it
                    var response = _webAgent.ExecuteRequest(next);

                    // log that we've crawled it
                    _crawled.Add(next);

                    var html = HTMLRetriever.GetHTML(_webAgent.GetCompressedStream(await response));

                    // parse the contents for new links and data user wants
                    var data = DataExtractor.Extract(html, job.Domain, job.Regex);

                    // add links found to queue if they're part of the domain and not already crawled and not already in the queue
                    // and not a disallowed url
                    // and make sure the queue is not too big
                    foreach (var link in data.Links)
                    {
                        if (_queue.Count < QUEUE_MAX && !_queue.Contains(link) && !_crawled.Contains(link))
                        {
                            _queue.Enqueue(link);
                        }
                    }

                    // add data matching the regex to the return list
                    foreach (var foundData in data.Data)
                    {
                        _results.Add(foundData);
                    }
                }
                catch (WebException e)
                {
                       
                }
            }

            if (!_aThreadIsComplete)
                _aThreadIsComplete = true;

            doneEvent.Set();
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
