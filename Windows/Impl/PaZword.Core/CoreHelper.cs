using PaZword.Core.Security.PasswordStrengthEvaluator;
using PaZword.Core.Threading;
using System;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace PaZword.Core
{
    /// <summary>
    /// Provides a set of methods used to get information about the application.
    /// </summary>
    public static class CoreHelper
    {
        private static readonly Zxcvbn _passwordStrengthEvaluator = new Zxcvbn();

        /// <summary>
        /// Gets whether an internet access is available.
        /// </summary>
        /// <returns>True is there is an internet access available.</returns>
        public static bool IsInternetAccess()
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            return connectionProfile != null && connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }

        /// <summary>
        /// Gets or creates the folder in the local application storage where the user's data are stored.
        /// </summary>
        /// <returns></returns>
        public static async Task<StorageFolder> GetOrCreateUserDataStorageFolderAsync()
        {
            return await ApplicationData.Current
                .LocalFolder
                .CreateFolderAsync(Constants.UserDataFolderName, CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Determines the strength of a given <paramref name="password"/> and returns a score on 100 and a suggested color brush associated.
        /// </summary>
        /// <remarks>
        /// This method should run on the UI thread.
        /// </remarks>
        /// <param name="password">The password to test</param>
        /// <returns>A score between 0 and 100, and a color brush or null</returns>
        public static (SolidColorBrush colorBrush, int strength) DeterminePasswordStrength(string password)
        {
            TaskHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(password))
            {
                return (null, 0);
            }

            double timeToCrack = _passwordStrengthEvaluator.EvaluatePasswordCrackTime(password);

            if (timeToCrack <= TimeSpan.FromHours(5).TotalSeconds) // 5h, Very Weak
            {
                return (new SolidColorBrush(Colors.DarkRed), 10);
            }
            else if (timeToCrack <= TimeSpan.FromDays(7).TotalSeconds) // 1 week, Weak
            {
                return (new SolidColorBrush(Colors.Red), 25);
            }
            else if (timeToCrack <= TimeSpan.FromDays(365).TotalSeconds) // 1 year, Medium
            {
                return (new SolidColorBrush(Colors.Orange), 50);
            }
            else if (timeToCrack <= TimeSpan.FromDays(365 * 100).TotalSeconds) // 1 century, Strong
            {
                return (new SolidColorBrush(Colors.Green), 75);
            }
            else // more than a year, Very Strong
            {
                return (new SolidColorBrush(Colors.DarkGreen), 100);
            }
        }

        /// <summary>
        /// Tries to run a given action and retry it in case of failure.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="maxTry">How many try are allowed.</param>
        /// <param name="delayBetweenTry">How long to wait between each try.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task RetryAsync(Action action, uint maxTry = 3, int delayBetweenTry = 1000)
        {
            var tryCount = 1;

            do
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception)
                {
                    if (tryCount == maxTry)
                    {
                        throw;
                    }

                    await Task.Delay(delayBetweenTry).ConfigureAwait(false);
                }

                tryCount++;
            } while (tryCount < maxTry);
        }

        /// <summary>
        /// Tries to run a given action and retry it in case of failure.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="maxTry">How many try are allowed.</param>
        /// <param name="delayBetweenTry">How long to wait between each try.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task RetryAsync(Func<Task> action, uint maxTry = 3, int delayBetweenTry = 1000)
        {
            var tryCount = 1;

            do
            {
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (Exception)
                {
                    if (tryCount == maxTry)
                    {
                        throw;
                    }

                    await Task.Delay(delayBetweenTry).ConfigureAwait(false);
                }

                tryCount++;
            } while (tryCount < maxTry);
        }

        /// <summary>
        /// Tries to run a given action and retry it in case of failure.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="maxTry">How many try are allowed.</param>
        /// <param name="delayBetweenTry">How long to wait between each try.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, uint maxTry = 3, int delayBetweenTry = 1000)
        {
            var tryCount = 1;

            do
            {
                try
                {
                    T result = await action().ConfigureAwait(false);
                    return result;
                }
                catch (Exception)
                {
                    if (tryCount == maxTry)
                    {
                        throw;
                    }

                    await Task.Delay(delayBetweenTry).ConfigureAwait(false);
                }

                tryCount++;
            } while (tryCount < maxTry);

            return default;
        }
    }
}
