using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YAC.Abstractions;

namespace YAC
{
    public class Worker : IWorker
    {
        public int Id { get; }
        public bool HasWork => NextUri != null;
        public Uri NextUri { get; set; }
        public ManualResetEvent DoneEvent { get; }

        public Worker(int id, ManualResetEvent doneEvent)
        {
            Id = id;
            DoneEvent = doneEvent;
        }
    }
}
