using System;
using System.Collections.Generic;
using System.Text;
using YAC.Abstractions;
using YAC.Models;

namespace YAC.CrawlCompletionConditions
{
    public class MaxErrorsCondition : ICrawlCompletionCondition
    {
        private readonly int _maxErrors;

        public MaxErrorsCondition(int maxErrors)
        {
            _maxErrors = maxErrors;
        }

        public bool ConditionMet(CrawlProgress progress)
        {
            return progress.Exceptions.Count >= _maxErrors;
        }
    }
}
