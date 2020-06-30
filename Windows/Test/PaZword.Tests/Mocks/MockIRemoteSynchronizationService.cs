using PaZword.Api.Data;
using PaZword.Core.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Mocks
{
    internal sealed class MockIRemoteSynchronizationService : IRemoteSynchronizationService, IDisposable
    {
        private readonly DisposableSempahore _sempahore = new DisposableSempahore();

        private CancellationTokenSource _cancellationTokenSource;

        public bool IsSynchronizing => _sempahore.IsBusy;

        public event EventHandler<EventArgs> SynchronizationStarted;
        public event EventHandler<SynchronizationResultEventArgs> SynchronizationCompleted;

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _sempahore.Dispose();
        }

        public void QueueSynchronization()
        {
            lock (_sempahore)
            {
                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                SynchronizeAsync(_cancellationTokenSource.Token).Forget();
            }
        }

        public void Cancel()
        {
            lock (_sempahore)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            // The semaphore acts like queue.
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                SynchronizationStarted?.Invoke(this, EventArgs.Empty);
                SynchronizationCompleted?.Invoke(this, new SynchronizationResultEventArgs(true, true));
            }
        }
    }
}
