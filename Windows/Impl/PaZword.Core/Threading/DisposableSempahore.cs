using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Core.Threading
{
    internal sealed class DisposableSempahore : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        internal bool IsBusy => _semaphore.CurrentCount == 0;

        public DisposableSempahore(int maxTasksCount = 1)
        {
            _semaphore = new SemaphoreSlim(maxTasksCount, maxTasksCount);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        internal async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new DummyDisposable(_semaphore);
        }

        private sealed class DummyDisposable : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public DummyDisposable(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
