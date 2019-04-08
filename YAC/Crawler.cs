﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YAC.Abstractions;

namespace YAC
{
    public class Crawler : ICrawler
    {
        private readonly IWebAgent _webAgent;

        private bool _aThreadIsComplete;

        public Crawler(IWebAgent webAgent)
        {
            _webAgent = webAgent;
        }

        public Task<IEnumerable<string>> Crawl(CrawlJob job)
            => Task.Run<IEnumerable<string>>(() =>
            {
                // create the allowed amount of threads for the job

                // try and parse the robots.txt file of the domain and add any disallowed links to a read only collection

                // hold all but one thread in a pattern until there is work for them

                // start the first thread off, with the job of parsing the domain page provided

                // once work comes in for each thread, release from the holding pattern and allow them to work

                // each thread should while loop until: any of the completion conditions are met or the cancellation token is used

                // upon completion, the thread that first completed should flag that the other threads need to complete too,
                // even those stuck in the initial holding pattern (if any)

                // flush queues and return the list of data found during the crawl
                return new List<string>();
            });

        private void ThreadAction(ManualResetEvent doneEvent, IEnumerable<ICrawlCompletionCondition> completionConditions, CancellationToken cancellationToken)
        {
            do
            {
                
            } while (completionConditions.All(cc => !cc.ConditionMet()) && !cancellationToken.IsCancellationRequested && !_aThreadIsComplete);

            doneEvent.Set();
            if (!_aThreadIsComplete)
                _aThreadIsComplete = true;
        }

        public void Dispose()
        {
            
            throw new NotImplementedException();
        }
    }
}
