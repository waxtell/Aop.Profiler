using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    public abstract class EventBatchProcessorBase : IProcessProfilerEvents, IDisposable
    {
        private readonly int _batchSizeLimit;
        private readonly TimeSpan _period;
        private readonly BoundedConcurrentQueue<IDictionary<string,object>> _queue;
        private readonly Queue<IDictionary<string, object>> _waitingBatch = new Queue<IDictionary<string, object>>();
        private readonly object _stateLock = new object();
        private readonly WakeTimer _timer;
        private bool _unloading;

        protected EventBatchProcessorBase(int batchSizeLimit, TimeSpan period, uint queueLimit)
        {
            _batchSizeLimit = batchSizeLimit;
            _timer = new WakeTimer(cancel => ProcessBatch());
            _period = period;
            _queue = new BoundedConcurrentQueue<IDictionary<string,object>>(queueLimit);

            _timer.Start(period);
        }

        private void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (_unloading)
                {
                    return;
                }

                _unloading = true;
            }

            _timer.Dispose();

            ResetSyncContextAndWait(ProcessBatch);
        }

        private static void ResetSyncContextAndWait(Func<Task> taskFactory)
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

        public abstract Task ProcessEvents(Queue<IDictionary<string, object>> events);

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

        private async Task ProcessBatch()
        {
            try
            {
                bool wasFullBatch;

                do
                {
                    while (_waitingBatch.Count < _batchSizeLimit && _queue.TryDequeue(out var next))
                    {
                        _waitingBatch.Enqueue(next);
                    }

                    if (_waitingBatch.Count == 0)
                    {
                        return;
                    }

                    await ProcessEvents(_waitingBatch);

                    wasFullBatch = _waitingBatch.Count >= _batchSizeLimit;

                    _waitingBatch.Clear();
                }
                while (wasFullBatch);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Exceptions generated while performing method profiling should not
                // break the users' code.
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

        public void ProcessEvent(IDictionary<string,object> @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            // ReSharper disable once InconsistentlySynchronizedField
            if (_unloading)
            {
                return;
            }

            _queue.TryEnqueue(@event);
        }
    }
}
