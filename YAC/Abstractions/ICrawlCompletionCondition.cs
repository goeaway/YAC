using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Abstractions
{
    public interface ICrawlCompletionCondition
    {
        bool ConditionMet();
    }
}
