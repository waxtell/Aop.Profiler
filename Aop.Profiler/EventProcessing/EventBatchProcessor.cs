using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    public class EventBatchProcessor : EventBatchProcessorBase
    {
        private readonly Func<Queue<IDictionary<string, object>>, Task> _processEvents;

        public EventBatchProcessor(Func<Queue<IDictionary<string, object>>, Task> processEvents, int batchSizeLimit, TimeSpan period, uint queueLimit)
        : base(batchSizeLimit,period,queueLimit)
        {
            _processEvents = processEvents;
        }

        public override Task ProcessEvents(Queue<IDictionary<string, object>> events)
        {
            return _processEvents(events);
        }
    }
}
