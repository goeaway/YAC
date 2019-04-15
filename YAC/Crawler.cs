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
        private readonly TimeSpan _updateInterval;
        private readonly Action<CrawlProgress> _updateAction;

        private bool _aThreadIsComplete;
        private ConcurrentQueue<Uri> _queue;
        private ConcurrentBag<Uri> _crawled;
        private ConcurrentBag<Tuple<string, string>> _results;
        private ConcurrentBag<Exception> _errors;
        private IEnumerable<string> _disallowedUrls;
        private DateTime _startTime;
        private CancellationTokenSource _cancelSource;
        private DateTime _lastUpdate;

        public bool IsRunning { get; private set; }

        public Crawler(IWebAgent webAgent) : this(webAgent, cp => { }, TimeSpan.MaxValue)
        {
        }

        public Crawler(IWebAgent webAgent, Action<CrawlProgress> updateAction, TimeSpan updateInterval)
        {
            _webAgent = webAgent;
            _updateInterval = updateInterval;
            _updateAction = updateAction;
        }

        private void Setup(IReadOnlyCollection<Uri> seedUris, IList<Cookie> cookies)
        {
            // add the seeds to the queue first
            _queue = new ConcurrentQueue<Uri>(seedUris);
            _crawled = new ConcurrentBag<Uri>();
            _results = new ConcurrentBag<Tuple<string, string>>();
            _disallowedUrls = new List<string>();
            _errors = new ConcurrentBag<Exception>();
            _aThreadIsComplete = false;
            _cancelSource = new CancellationTokenSource();

            if(cookies != null && cookies.Count > 0)
                _webAgent.Cookies = cookies;
        }

        #region - Interface -

        public void Cancel()
        {
            if(!IsRunning) 
                throw new CrawlerNotRunningException();

            _cancelSource.Cancel();
        }

        public async Task<CrawlReport> Crawl(CrawlJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            IsRunning = true;

            try
            {
                Setup(job.SeedUris, job.Cookies);

                // try and parse the robots.txt file of the domain and add any disallowed links to a read only collection
                _disallowedUrls = await RobotParser.GetDisallowedUrls(_webAgent, job.Domain.Host);

                // quit early as we are not allowed to go on this domain
                if (_disallowedUrls.Contains("/"))
                    return GetCrawlReport();

                // create the allowed amount of threads for the job
                var threadsAndDoneEvents = CreateThreads(job);

                _startTime = DateTime.Now;
                _lastUpdate = _startTime;

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

        public void Dispose()
        {

        }

        #endregion

        #region - Private -

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

        private bool NeedsUpdate()
        {
            return (DateTime.Now - _lastUpdate) >= _updateInterval;
        }

        private async Task ThreadAction(IWorker worker, CrawlJob job)
        {
            // sort out multi threading holding pattern
            if (worker.Id != 0)
            {
                while (_queue.Count < (worker.Id + 1) && !_cancelSource.Token.IsCancellationRequested && !_aThreadIsComplete)
                {
                    Thread.Sleep(100);
                }
            }

            while (job.CompletionConditions.All(cc => !cc.ConditionMet(GetCrawlProgress())) && 
                   !_cancelSource.Token.IsCancellationRequested &&
                   !_aThreadIsComplete)
            {
                if (worker.Id == 0 && NeedsUpdate())
                {
                    _updateAction(GetCrawlProgress());
                }

                // set up fallback and retry policies
                var fallback = Policy<Uri>.Handle<CrawlQueueEmptyException>()
                    .Fallback((cToken) =>
                    {
                        _aThreadIsComplete = true;
                        return null;
                    });

                var retry = Policy<Uri>.Handle<CrawlQueueEmptyException>()
                    .WaitAndRetry(10, tryNum => TimeSpan.FromMilliseconds(tryNum * 200));

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

                        // add each of the links extracted if:
                        // the queue is not too large
                        // the link is not disallowed by the domain's robots.txt file
                        // the link is not already in the queue
                        // the link has not already been crawled
                        // each of the user defined enqueue conditions returns true 
                        foreach (var link in data.Links)
                        {
                            if (_queue.Count < QUEUE_MAX && 
                                RobotParser.UriIsAllowed(_disallowedUrls, link) && 
                                !_queue.Contains(link) && 
                                !_crawled.Contains(link) && 
                                job.EnqueueConditions.All(ec => ec.ConditionMet(link)))
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

        #endregion
    }
}