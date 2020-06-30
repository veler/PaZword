using System;
using System.Text;

namespace PaZword.Api
{
    /// <summary>
    /// Provides a set of method to write logs.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Raised when the logs changes.
        /// </summary>
        event EventHandler LogsChanged;

        /// <summary>
        /// Retrieve all the logs.
        /// </summary>
        /// <returns>Returns a <see cref="StringBuilder"/> containing all the logs.</returns>
        StringBuilder GetAllLogs();

        /// <summary>
        /// Logs an informative event.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="description">Description of the event</param>
        void LogEvent(string eventName, string description);

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="description">Description of the issue</param>
        /// <param name="exception">The exception that thrown</param>
        void LogFault(string eventName, string description, Exception exception);
    }
}
