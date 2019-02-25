using System.Linq;
using Aop.Profiler.EventProcessing;
using Xunit;

namespace Aop.Profiler.Unit.Tests
{
    public class BoundedConcurrentQueueTests
    {
        [Fact]
        public void CannotExceedUpperBoundaryTest()
        {
            var queue = new BoundedConcurrentQueue<int>(10);

            Enumerable.Range(1,20).AsParallel().ForAll(x => queue.TryEnqueue(x));

            Assert.Equal(10, queue.Count);
        }
    }
}
