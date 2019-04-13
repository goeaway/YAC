using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;
using YAC.Models;

namespace YAC.CrawlCompletionConditions
{
    public class MaxResultsFoundCondition : ICrawlCompletionCondition
    {
        private readonly int _maxResultsCount;

        public MaxResultsFoundCondition(int maxResultsCount)
        {
            _maxResultsCount = maxResultsCount;
        }

        public bool ConditionMet(CrawlProgress progress)
        {
            return progress.ResultsCount >= _maxResultsCount;
        }
    }
}
