using System;
using System.Collections.Generic;
using System.Text;
using YAC.Models;

namespace YAC.Abstractions
{
    public interface ICrawlCompletionCondition
    {
        bool ConditionMet(CrawlProgress progress);
    }
}
