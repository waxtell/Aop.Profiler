using System.Collections.Generic;

namespace Aop.Profiler.EventProcessing
{
    public interface IProcessProfilerEvents
    {
        void ProcessEvent(IDictionary<string, object> @event);
    }
}