using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    public class EventBatchProcessor : IProcessProfilerEvents, IDisposable
    {
        private readonly int _batchSizeLimit;
        private readonly TimeSpan _period;
        private readonly BoundedConcurrentQueue<IDictionary<string,object>> _queue;
        private readonly Queue<IDictionary<string, object>> _waitingBatch = new Queue<IDictionary<string, object>>();
        private readonly object _stateLock = new object();
        private readonly WakeTimer _timer;
        private bool _unloading;
        private readonly Func<Queue<IDictionary<string, object>>, Task> _processEvents;

        public EventBatchProcessor(Func<Queue<IDictionary<string, object>>, Task> processEvents, int batchSizeLimit, TimeSpan period, uint queueLimit)
        {
            _processEvents = processEvents;
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

                    await _processEvents(_waitingBatch);

                    wasFullBatch = _waitingBatch.Count >= _batchSizeLimit;

                    _waitingBatch.Clear();
                }
                while (wasFullBatch);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
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

            _queue.TryEnqueue(logEvent);
        }
    }
}
