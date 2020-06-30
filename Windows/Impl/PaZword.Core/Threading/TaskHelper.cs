using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PaZword.Core.Threading
{
    /// <summary>
    /// Provides a set of helper method to play around with threads.
    /// </summary>
    public static class TaskHelper
    {
        private readonly static CoreDispatcher _uiDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

        /// <summary>
        /// Throws an exception if the current thread isn't the UI thread.
        /// </summary>
        public static void ThrowIfNotOnUIThread()
        {
            if (!_uiDispatcher.HasThreadAccess)
            {
                throw new Exception("The UI thread is expected, but the current call stack is running on another thread.");
            }
        }

        /// <summary>
        /// Throws an exception if the current thread is the UI thread.
        /// </summary>
        public static void ThrowIfOnUIThread()
        {
            if (_uiDispatcher.HasThreadAccess)
            {
                throw new Exception("The UI thread is not expected, but the current call stack is running on UI thread.");
            }
        }

        /// <summary>
        /// Runs a given action on the UI thread and wait for its result asynchronously.
        /// </summary>
        /// <param name="action">Action to run on the UI thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task RunOnUIThreadAsync(DispatchedHandler action)
        {
            if (action == null)
            {
                return Task.CompletedTask;
            }

            if (_uiDispatcher.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }
            else
            {
                return _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, action).AsTask();
            }
        }

        /// <summary>
        /// Runs a given action on the UI thread and wait for its result asynchronously.
        /// </summary>
        /// <param name="action">Action to run on the UI thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (action == null)
            {
                return;
            }

            if (_uiDispatcher.HasThreadAccess)
            {
                await action().ConfigureAwait(true);
            }
            else
            {
                var tcs = new TaskCompletionSource<object>();
                await _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        await action().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        tcs.SetResult(null);
                    }
                });

                await tcs.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs a given action on the UI thread and wait for its result asynchronously.
        /// </summary>
        /// <param name="action">Action to run on the UI thread.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
            {
                return default;
            }

            if (_uiDispatcher.HasThreadAccess)
            {
                return await action().ConfigureAwait(true);
            }
            else
            {
                T result = default;
                var tcs = new TaskCompletionSource<object>();
                _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        result = await action().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        tcs.TrySetResult(null);
                    }
                }).AsTask().ForgetSafely();

                await tcs.Task.ConfigureAwait(false);
                return result;
            }
        }

        /// <summary>
        /// Runs a task without waiting for its result.
        /// </summary>
        public static void Forget(this Task _)
        {
        }

        /// <summary>
        /// Runs a task without waiting for its result.
        /// </summary>
        public static void Forget<T>(this Task<T> _)
        {
        }

        /// <summary>
        /// Runs a task without waiting for its result. Swallows or handle any exception caused by the task.
        /// </summary>
        /// <param name="errorHandler">The action to run when an exception is caught.</param>
        public static async void ForgetSafely(this Task task, Action<Exception> errorHandler = null)
        {
            try
            {
                await task.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }
    }
}
