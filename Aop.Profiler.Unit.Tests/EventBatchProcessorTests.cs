using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aop.Profiler.EventProcessing;
using Xunit;

namespace Aop.Profiler.Unit.Tests
{
    public class EventBatchProcessorTests
    {
        [Fact]
        public void ProcessorProcessesProperBatchSizeAndCount()
        {
            var queueSizes = new List<int>();

            using (var processor = new EventBatchProcessor(ProcessEvents, 10, TimeSpan.FromSeconds(100), 50))
            {
                Enumerable
                    .Range(1, 20)
                    .AsParallel()
                    .ForAll
                    (
                        x =>
                        {
                            // ReSharper disable AccessToDisposedClosure
                            processor.ProcessEvent(new Dictionary<string, object> {{x.ToString(), x}});
                            // ReSharper disable AccessToDisposedClosure
                        }
                    );
            }

            Assert.True(queueSizes.All(x => x == 10));
            Assert.Equal(2,queueSizes.Count);

#pragma warning disable 1998
            async Task ProcessEvents(Queue<IDictionary<string, object>> queue) => queueSizes.Add(queue.Count);
#pragma warning restore 1998
        }

        [Fact]
        public void ProcessorProcessesIncompleteBatchOnShutdown()
        {
            var queueSizes = new List<int>();

            using (var processor = new EventBatchProcessor(ProcessEvents, 10, TimeSpan.FromSeconds(100), 50))
            {
                Enumerable
                    .Range(1, 9)
                    .AsParallel()
                    .ForAll
                    (
                        x =>
                        {
                            // ReSharper disable AccessToDisposedClosure
                            processor.ProcessEvent(new Dictionary<string, object> { { x.ToString(), x } });
                            // ReSharper disable AccessToDisposedClosure
                        }
                    );
            }

            Assert.True(queueSizes.All(x => x == 9));
            Assert.Single(queueSizes);

#pragma warning disable 1998
            async Task ProcessEvents(Queue<IDictionary<string, object>> queue) => queueSizes.Add(queue.Count);
#pragma warning restore 1998
        }
    }
}
