﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YAC.Abstractions
{
    public interface IRateLimiter
    {
        void HoldUntilReady(string domain);
        bool CanAccess(string domain);
    }
}
