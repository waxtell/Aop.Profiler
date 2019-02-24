using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aop.Profiler.EventProcessing;
using Xunit;

namespace Aop.Profiler.Unit.Tests
{
    public class UnitTest1 : IProcessProfilerEvents
    {
        [Fact]
        public async Task Test1()
        {
            var proxy = new PerInstanceAdapter<IForTestingPurposes>(new ForTestingPurposes(),this).Object;

            proxy.MethodCall(0, "zero");

            await proxy.AsyncAction(0, "zero");
            await proxy.AsyncMethodCall(0, "zero");

            _ = proxy.Member;
        }

        public void ProcessEvent(IDictionary<string, object> profilerEvent)
        {
            foreach (var key in profilerEvent.Keys)
            {
                Console.WriteLine("{0}:{1}",key,profilerEvent[key]);
            }
        }
    }
}
