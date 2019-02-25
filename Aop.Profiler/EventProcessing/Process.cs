using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    public static class Process
    {
        public static IProcessProfilerEvents Lean(Action<IDictionary<string, object>> leanHandler)
        {
            return new LeanProcessor(leanHandler);
        }

        public static IProcessProfilerEvents Batch(Func<Queue<IDictionary<string, object>>,Task> batchHandler, int batchSize, TimeSpan interval, uint maxQueueSize)
        {
            return new EventBatchProcessor(batchHandler,batchSize, interval, maxQueueSize);
        }
    }
}
