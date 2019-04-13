using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;
using YAC.Models;

namespace YAC.CrawlCompletionConditions
{
    public class MaxPagesCrawledCondition : ICrawlCompletionCondition
    {
        private readonly int _maxCrawlCount;

        public MaxPagesCrawledCondition(int maxCrawlCount)
        {
            _maxCrawlCount = maxCrawlCount;
        }

        public bool ConditionMet(CrawlProgress progress)
        {
            return progress.CrawlCount >= _maxCrawlCount;
        }
    }
}
