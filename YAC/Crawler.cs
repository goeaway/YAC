using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using YAC.Abstractions;
using YAC.Exceptions;
using YAC.Models;
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
        private ConcurrentBag<Exception> _errors;
        private IEnumerable<string> _disallowedUrls;
        private DateTime _startTime;

        public bool IsRunning { get; private set; }

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
            _errors = new ConcurrentBag<Exception>();
            _aThreadIsComplete = false;

        }

        public async Task<CrawlReport> Crawl(CrawlJob job)
        {
            Setup(job.SeedUris);

            // try and parse the robots.txt file of the domain and add any disallowed links to a read only collection
            _disallowedUrls = await RobotParser.GetDisallowedUrls(_webAgent, job.Domain.Host);

            // quit early as we are not allowed to go on this domain
            if (_disallowedUrls.Contains("/"))
                return GetCrawlReport();

            // create the allowed amount of threads for the job
            var threadsAndDoneEvents = CreateThreads(job);

            _startTime = DateTime.Now;
            IsRunning = true;

            try
            {
                // hold all but one thread in a pattern until there is work for them
                // start the first thread off, with the job of parsing the domain page provided
                foreach (var thread in threadsAndDoneEvents.Item1)
                {
                    thread.Start();
                }

                // wait for done events
                WaitHandle.WaitAll(threadsAndDoneEvents.Item2.ToArray());

                // flush queues and return the list of data found during the crawl
                foreach (var thread in threadsAndDoneEvents.Item1)
                {
                    if (thread.ThreadState == ThreadState.Running)
                        thread.Join();
                }

                return GetCrawlReport();
            }
            catch (Exception e)
            {
                throw new YACException("Exception thrown from YAC", e);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private Tuple<List<Thread>, List<ManualResetEvent>> CreateThreads(CrawlJob job)
        {
            var threads = new List<Thread>();
            var events = new List<ManualResetEvent>();

            for (int i = 0; i < Math.Max(1, job.ThreadAllowance); i++)
            {
                var doneEvent = new ManualResetEvent(false);
                var id = i;
                var thread = new Thread(async () => await ThreadAction(new Worker(id, doneEvent), job))
                {
                    Name = "YAC Worker"
                };

                events.Add(doneEvent);
                threads.Add(thread);
            }

            return new Tuple<List<Thread>, List<ManualResetEvent>>(threads, events);
        }

        private CrawlReport GetCrawlProgress()
        {
            return new CrawlReport
            {
                Start = _startTime,
                CrawlCount = _crawled.Count,
                ResultsCount = _results.Count,
                QueueSize =  _queue.Count,
                Exceptions = _errors.ToList()
            };
        }

        private CrawlReport GetCrawlReport()
        {
            var report = GetCrawlProgress();
            report.Data = _results;
            return report;
        }

        private async Task ThreadAction(IWorker worker, CrawlJob job)
        {
            // sort out multi threading holding pattern
            if (worker.Id != 0)
            {
                while (_queue.Count < (worker.Id + 1) && !job.CancellationToken.IsCancellationRequested && !_aThreadIsComplete)
                {
                    Thread.Sleep(100);
                }
            }

            while (job.CompletionConditions.All(cc => !cc.ConditionMet(GetCrawlProgress())) && 
                   !job.CancellationToken.IsCancellationRequested &&
                   !_aThreadIsComplete)
            {
                // set up fallback and retry policies
                var fallback = Policy<Uri>.Handle<CrawlQueueEmptyException>()
                    .Fallback((cToken) =>
                    {
                        _aThreadIsComplete = true;
                        return null;
                    });

                var retry = Policy<Uri>.Handle<CrawlQueueEmptyException>()
                    .WaitAndRetry(30, tryNum => TimeSpan.FromMilliseconds(tryNum * 200));

                // will attempt to get a new item from the queue, retrying as per above policies
                var next = Policy.Wrap(fallback, retry).Execute(() =>
                {
                    var n = GetNext();

                    if (n == null)
                        throw new CrawlQueueEmptyException();

                    return n;
                });

                // fallback will set this if we failed to get a new link (this will end the crawl)
                if (_aThreadIsComplete)
                    continue;

                try
                {
                    // access it
                    var responseTask = _webAgent.ExecuteRequest(next);

                    // log that we've crawled it
                    _crawled.Add(next);

                    var response = await responseTask;

                    if (response != null)
                    {
                        var html = HTMLRetriever.GetHTML(_webAgent.GetCompressedStream(response));

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
                }
                catch (WebException e)
                {
                    _errors.Add(e);
                }
            }

            if (!_aThreadIsComplete)
                _aThreadIsComplete = true;

            worker.DoneEvent.Set();
        }

        private Uri GetNext()
        {
            if (_queue.IsEmpty)
                return null;

            if (!_queue.TryDequeue(out Uri next))
                next = GetNext();

            return next;
        }

        public void Dispose()
        {

        }
    }
}
