using System;

namespace PaZword.Api.Services
{
    /// <summary>
    /// Provides a service that runs small or long recurrent tasks.
    /// </summary>
    public interface IRecurrentTaskService
    {
        /// <summary>
        /// Raised when a <see cref="IRecurrentTask"/> completed.
        /// </summary>
        event EventHandler<RecurrentTaskEventArgs> TaskCompleted;

        /// <summary>
        /// Starts the service and runs the recurrent tasks when necessary.
        /// </summary>
        void Start();

        /// <summary>
        /// Cancels running tasks and stop running them until <see cref="Start"/> is invoked.
        /// </summary>
        void Pause();

        /// <summary>
        /// Starts a given <paramref name="taskName"/>.
        /// </summary>
        /// <remarks>
        /// It won't start the task if the service is paused.
        /// </remarks>
        /// <param name="taskName">The name of the <see cref="IRecurrentTask"/> to start.</param>
        void RunTaskExplicitly(string taskName);
    }
}
