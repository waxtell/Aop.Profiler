using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    public abstract class EventBatcher9000 : IProcessProfilerEvents, IDisposable
    {
        private readonly int _batchSizeLimit;
        private readonly TimeSpan _period;
        private readonly BoundedConcurrentQueue<IDictionary<string,object>> _queue;
        private readonly Queue<IDictionary<string, object>> _waitingBatch = new Queue<IDictionary<string, object>>();
        private readonly object _stateLock = new object();
        private readonly PortableTimer _timer;
        private bool _unloading;
        private bool _started;
        private readonly Func<IList<IDictionary<string, object>>, Task> _batchDelegate;

        //protected EventBatcher9000(uint batchSizeLimit, TimeSpan period)
        //{
        //    _batchSizeLimit = batchSizeLimit;
        //    _queue = new BoundedConcurrentQueue<IDictionary<string,object>>(batchSizeLimit);
            
        //    _timer = new PortableTimer(cancel => OnTick());
        //    _period = period;
        //}

        protected EventBatcher9000(int batchSizeLimit, TimeSpan period, uint queueLimit)
            //: this(batchSizeLimit, period)
        {
            _batchSizeLimit = batchSizeLimit;
            _timer = new PortableTimer(cancel => OnTick());
            _period = period;
            _queue = new BoundedConcurrentQueue<IDictionary<string,object>>(queueLimit);
        }

        private void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (!_started || _unloading)
                {
                    return;
                }

                _unloading = true;
            }

            _timer.Dispose();

            ResetSyncContextAndWait(OnTick);
        }

        private void ResetSyncContextAndWait(Func<Task> taskFactory)
        {
            var prevContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            try
            {
                taskFactory().Wait();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            CloseAndFlush();
        }

        protected virtual void EmitBatch(IEnumerable<IDictionary<string,object>> events)
        {
        }

        protected virtual async Task EmitBatchAsync(IEnumerable<IDictionary<string,object>> events)
        {
            EmitBatch(events);
        }

        private async Task OnTick()
        {
            try
            {
                bool batchWasFull;

                do
                {
                    IDictionary<string,object> next;

                    while (_waitingBatch.Count < _batchSizeLimit && _queue.TryDequeue(out next))
                    {
                        _waitingBatch.Enqueue(next);
                    }

                    if (_waitingBatch.Count == 0)
                    {
                        return;
                    }

                    await EmitBatchAsync(_waitingBatch);

                    batchWasFull = _waitingBatch.Count >= _batchSizeLimit;

                    _waitingBatch.Clear();
                }
                while (batchWasFull); // Otherwise, allow the period to elapse
            }
            catch (Exception ex)
            {
            }
            finally
            {
                lock (_stateLock)
                {
                    if (!_unloading)
                    {
                        SetTimer(_period);
                    }
                }
            }
        }

        private void SetTimer(TimeSpan interval)
        {
            _timer.Start(interval);
        }

        public void ProcessEvent(IDictionary<string,object> logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (_unloading)
            {
                return;
            }

            if (!_started)
            {
                lock (_stateLock)
                {
                    if (_unloading)
                    {
                        return;
                    }

                    if (!_started)
                    {
                        _started = true;
                    }
                }
            }

            _queue.TryEnqueue(logEvent);
        }
    }
}
