using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aop.Profiler.EventProcessing
{
    internal class PortableTimer : IDisposable
    {
        private readonly object _stateLock = new object();
        private readonly Func<CancellationToken, Task> _onTick;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        private bool _running;
        private bool _disposed;

        public PortableTimer(Func<CancellationToken, Task> onTick)
        {
            _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
        }

        public void Start(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            lock (_stateLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PortableTimer));
                }

                Task
                    .Delay(interval, _cancel.Token)
                    .ContinueWith
                    (
                        _ => OnTick(),
                        CancellationToken.None,
                        TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.Default
                    );
            }
        }

        private async void OnTick()
        {
            try
            {
                lock (_stateLock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    if (_running)
                    {
                        Monitor.Wait(_stateLock);

                        if (_disposed)
                        {
                            return;
                        }
                    }                    

                    _running = true;
                }

                if (!_cancel.Token.IsCancellationRequested)
                {
                    await _onTick(_cancel.Token);
                }
            }
            finally
            {
                lock (_stateLock)
                {
                    _running = false;
                    Monitor.PulseAll(_stateLock);
                }
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();
            
            lock (_stateLock)
            {
                if (_disposed)
                {
                    return;
                }

                while (_running)
                {
                    Monitor.Wait(_stateLock);
                }

                _disposed = true;
            }
        }
    }
}