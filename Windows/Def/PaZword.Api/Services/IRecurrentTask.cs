using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Api.Services
{
    /// <summary>
    /// Represents a task to run regularly.
    /// </summary>
    public interface IRecurrentTask
    {
        /// <summary>
        /// Determines whether the task can run or not. This method is called just before executing the task.
        /// </summary>
        /// <returns>Returns <code>true</code> if the <see cref="ExecuteAsync"/> should be called.</returns>
        Task<bool> CanExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>Returns any value that can be used by the application to, for example, notify the user of something.</returns>
        Task<object> ExecuteAsync(CancellationToken cancellationToken);
    }
}
