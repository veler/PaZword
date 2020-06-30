using System;

namespace PaZword.Api.Data
{
    /// <summary>
    /// Provides a service that synchronizes the user local data with the cloud.
    /// </summary>
    public interface IRemoteSynchronizationService
    {
        /// <summary>
        /// Gets whether the synchronization with the Cloud is in progress or not.
        /// </summary>
        bool IsSynchronizing { get; }

        /// <summary>
        /// Raised when the synchronization is enabled and just started.
        /// </summary>
        event EventHandler<EventArgs> SynchronizationStarted;

        /// <summary>
        /// Raised when the synchronization is completed (with error or not).
        /// </summary>
        event EventHandler<SynchronizationResultEventArgs> SynchronizationCompleted;

        /// <summary>
        /// Queue a task to synchronize the local data with the remote server.
        /// </summary>
        void QueueSynchronization();

        /// <summary>
        /// Cancels the current synchronization and clear the queue.
        /// </summary>
        void Cancel();
    }
}
