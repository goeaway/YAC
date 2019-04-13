using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;
using YAC.Models;

namespace YAC.CrawlCompletionConditions
{
    public class MaxTimeCondition : ICrawlCompletionCondition
    {
        private readonly TimeSpan _maxTime;

        public MaxTimeCondition(TimeSpan maxTime)
        {
            _maxTime = maxTime;
        }

        public bool ConditionMet(CrawlProgress progress)
        {
            return progress.CrawlDuration >= _maxTime;
        }
    }
}
