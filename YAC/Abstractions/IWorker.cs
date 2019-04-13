using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YAC.Abstractions
{
    public interface IWorker
    {
        int Id { get; }
        bool HasWork { get; }
        Uri NextUri { get; set; }
        ManualResetEvent DoneEvent { get; }
    }
}
