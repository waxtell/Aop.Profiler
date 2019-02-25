using System;
using System.Collections.Generic;

namespace Aop.Profiler.EventProcessing
{
    public class LeanProcessor : IProcessProfilerEvents
    {
        private readonly Action<IDictionary<string, object>> _processEvent;

        public LeanProcessor(Action<IDictionary<string, object>> processEvent)
        {
            _processEvent = processEvent;
        }

        public void ProcessEvent(IDictionary<string,object> @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            _processEvent(@event);
        }
    }
}
