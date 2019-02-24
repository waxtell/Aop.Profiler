using System.Collections.Concurrent;
using System.Threading;

namespace Aop.Profiler.EventProcessing
{
    internal class BoundedConcurrentQueue<T> 
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly uint _queueLimit;
        private int _counter;

        public BoundedConcurrentQueue(uint queueLimit)
        {
            _queueLimit = queueLimit;
        }

        public bool TryDequeue(out T item)
        {
            var result = false;

            if (_queue.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _counter);
                result = true;
            }

            return result;
        }

        public bool TryEnqueue(T item)
        {
            var result = true;

            if (Interlocked.Increment(ref _counter) <= _queueLimit)
            {
                _queue.Enqueue(item);
            }
            else
            {
                Interlocked.Decrement(ref _counter);
                result = false;
            }

            return result;
        }
    }
}
