using System.Collections.Concurrent;
using System.Threading;

namespace Aop.Profiler.EventProcessing
{
    internal class BoundedConcurrentQueue<T> 
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly uint _maxCapacity;

        public int Count => _itemCount;
        private volatile int _itemCount;

        public BoundedConcurrentQueue(uint maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        public bool TryDequeue(out T item)
        {
            var result = false;

            if (_queue.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _itemCount);
                result = true;
            }

            return result;
        }

        public bool TryEnqueue(T item)
        {
            var result = true;

            if (Interlocked.Increment(ref _itemCount) <= _maxCapacity)
            {
                _queue.Enqueue(item);
            }
            else
            {
                Interlocked.Decrement(ref _itemCount);
                result = false;
            }

            return result;
        }
    }
}
