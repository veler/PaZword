using PaZword.Api;
using PaZword.Api.Collections;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Services;
using PaZword.Core.Threading;
using PaZword.Models.Data;
using PaZword.Models.Pwned;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Core.RecurrentTasks
{
    [Export(typeof(IRecurrentTask))]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Name), Constants.PwnedRecurrentTask)]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Recurrency), TaskRecurrency.OneDay)]
    [Shared()]
    internal sealed class PwnedRecurrentTask : IRecurrentTask
    {
        private const string ExecutePwnedRecurrentTaskFaultEvent = "RecurrentTask.Pwned.Execute.Fault";

        // TODO: This is quite dirty. Use HaveIBeenPawned paid API instead, if this app gets remunerated.

        private const string BrownBagPwnedPrefix = "Pwned_";
        private const string FirefoxMonitorUrl = "https://monitor.firefox.com/";
        private const string FirefoxMonitorScanUrl = "https://monitor.firefox.com/scan";
        private const string CrsfIdentifier = "name=\"_csrf\" value=\"";
        private const string BreachInfoWrapperIdentifier = "breach-info-wrapper";
        private const string BreachInfoWrapperEndIdentifier = "</div>";
        private const string BreachTitleIdentifier = "breach-title\">";
        private const string BreachDateIdentifier = "breach-value\">";
        private const string BreachPasswordIdentifier = "breach-value\">Passwords";
        private const string BreachInfoEndIdentifier = "</span>";
        private const int DelayBetweenRequest = 10000;

        private readonly ILogger _logger;
        private readonly IDataManager _dataManager;
        private readonly ISerializationProvider _serializationProvider;
        private readonly object _lock = new object();

        private List<SecureString> _emailsToVerify;

        [ImportingConstructor]
        public PwnedRecurrentTask(
            ILogger logger,
            IDataManager dataManager,
            ISerializationProvider serializationProvider)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
        }

        public async Task<bool> CanExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CoreHelper.IsInternetAccess())
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            ConcurrentObservableCollection<Account> accounts
                = await _dataManager.SearchAsync(
                    new Guid(Constants.CategoryAllId),
                    string.Empty,
                    cancellationToken)
                .ConfigureAwait(false);

            var emailsToVerify = new List<SecureString>();

            try
            {

                for (int i = 0; i < accounts.Count; i++)
                {
                    Account account = accounts[i];
                    for (int j = 0; j < account.Data.Count; j++)
                    {
                        AccountData accountData = account.Data[j];
                        if (accountData is CredentialData credentialData)
                        {
                            SecureString email = credentialData.EmailAddress;
                            if (email.Length > 0 && !emailsToVerify.Any(item => email.IsEqualTo(item)))
                            {
                                emailsToVerify.Add(email);
                            }
                            else
                            {
                                email.Dispose();
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
            catch
            {
                foreach (SecureString email in emailsToVerify)
                {
                    email.Dispose();
                }

                emailsToVerify.Clear();

                throw;
            }

            lock (_lock)
            {
                _emailsToVerify = emailsToVerify;
            }

            return emailsToVerify.Count > 0;
        }

        public async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            var iteration = 1;
            var result = new List<Breach>();
            var brownBagChanged = false;

            try
            {
                var treatedBreaches = new HashSet<string>();
                IReadOnlyDictionary<string, string> pwnedBrownBagData
                    = await _dataManager.GetBrownBagDataAsync(
                        (name) => name.StartsWith(BrownBagPwnedPrefix, StringComparison.Ordinal),
                        cancellationToken)
                    .ConfigureAwait(false);

                for (int i = 0; i < _emailsToVerify.Count; i++)
                {
                    if (i > 0)
                    {
                        // Adding this delay due to API limit rate.
                        await Task.Delay(DelayBetweenRequest).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    SecureString emailAddress = _emailsToVerify[i];
                    string html = await GetBreachesWebPageAsync(emailAddress, cancellationToken).ConfigureAwait(false);

                    IReadOnlyList<Breach> breaches = await ParseBreachesAsync(
                        emailAddress,
                        html,
                        treatedBreaches,
                        pwnedBrownBagData,
                        cancellationToken)
                        .ConfigureAwait(false);

                    if (breaches.Count > 0)
                    {
                        brownBagChanged = true;
                        result.AddRange(breaches);
                    }

                    iteration++;
                }

                foreach (string key in pwnedBrownBagData.Keys)
                {
                    if (!treatedBreaches.Contains(key))
                    {
                        await _dataManager.SetBrownBagDataAsync(key, null, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogFault(ExecutePwnedRecurrentTaskFaultEvent, $"Unable to check whether an email address is compromised or not on iteration {iteration}.", ex);
            }
            finally
            {
                lock (_lock)
                {
                    foreach (SecureString email in _emailsToVerify)
                    {
                        email.Dispose();
                    }

                    _emailsToVerify.Clear();
                }

                if (brownBagChanged && !cancellationToken.IsCancellationRequested)
                {
                    _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
                }
            }

            return result;
        }

        /// <summary>
        /// Query Mozilla's FireFox Monitor website to determine if the given email address has been pwned.
        /// </summary>
        private async Task<string> GetBreachesWebPageAsync(SecureString emailAddress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = new CookieContainer();
                handler.UseCookies = true;

                using (var httpClient = new HttpClient(handler))
                {
                    var html = string.Empty;
                    using (HttpResponseMessage response = await httpClient.GetAsync(new Uri(FirefoxMonitorUrl), cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Unable to load the page: {response.StatusCode.ToString()}");
                        }

                        html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                    int crsfLocation = html.IndexOf(CrsfIdentifier, StringComparison.OrdinalIgnoreCase);
                    if (crsfLocation < 0)
                    {
                        throw new Exception("Unable to find CSRF token.");
                    }

                    crsfLocation += CrsfIdentifier.Length;

                    string csrf = html.Substring(crsfLocation, html.IndexOf("\"", crsfLocation, StringComparison.Ordinal) - crsfLocation);
                    string emailAddressSha1 = _serializationProvider.GetSha1Hash(emailAddress.ToUnsecureString());

                    var httpPostParameters = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("_csrf", csrf),
                        new KeyValuePair<string, string>("pageToken", string.Empty),
                        new KeyValuePair<string, string>("scannedEmailId", "1"),
                        new KeyValuePair<string, string>("email", emailAddress.ToUnsecureString()),
                        new KeyValuePair<string, string>("emailHash", emailAddressSha1)
                    };

                    using (var content = new FormUrlEncodedContent(httpPostParameters))
                    using (HttpResponseMessage response = await httpClient.PostAsync(new Uri(FirefoxMonitorScanUrl), content, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Unable to load the result page: {response.StatusCode.ToString()}");
                        }

                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Parse the given HTML code coming from Mozilla's FireFox monitor website
        /// and return only the breach that haven't been announced to the user yet.
        /// </summary>
        private async Task<IReadOnlyList<Breach>> ParseBreachesAsync(
            SecureString emailAddress,
            string html,
            HashSet<string> treatedBreaches,
            IReadOnlyDictionary<string, string> pwnedBrownBagData,
            CancellationToken cancellationToken)
        {
            var breachesSummary = new List<Breach>();

            int locationStart = -1;
            int locationEnd;
            do
            {
                locationStart = html.IndexOf(BreachInfoWrapperIdentifier, locationStart + 1, StringComparison.OrdinalIgnoreCase);
                if (locationStart > -1)
                {
                    locationEnd = html.IndexOf(BreachInfoWrapperEndIdentifier, locationStart, StringComparison.OrdinalIgnoreCase);
                    if (locationEnd > locationStart
                        && html.IndexOf(BreachPasswordIdentifier, locationStart, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        // Breach title
                        int breachTitleStart = html.IndexOf(BreachTitleIdentifier, locationStart, StringComparison.OrdinalIgnoreCase) + BreachTitleIdentifier.Length;
                        int breachTitleEnd = html.IndexOf(BreachInfoEndIdentifier, breachTitleStart, StringComparison.OrdinalIgnoreCase);
                        string breachTitle = html.Substring(breachTitleStart, breachTitleEnd - breachTitleStart);

                        // Breach date
                        int breachDateStart = html.IndexOf(BreachDateIdentifier, breachTitleEnd, StringComparison.OrdinalIgnoreCase) + BreachDateIdentifier.Length;
                        int breachDateEnd = html.IndexOf(BreachInfoEndIdentifier, breachDateStart, StringComparison.OrdinalIgnoreCase);
                        string breachDateStr = html.Substring(breachDateStart, breachDateEnd - breachDateStart);

                        var brownBagItemKey = $"{BrownBagPwnedPrefix}{breachTitle}_{_serializationProvider.GetSha1Hash(emailAddress.ToUnsecureString())}";
                        var breachDate = DateTime.Parse(breachDateStr, new CultureInfo("en"));

                        treatedBreaches.Add(brownBagItemKey);

                        // If this breach has never been recorded in the user data bundle,
                        // or that the detected breach is more recent that the one we had in record,
                        // then we return a result, so the user will be notified.
                        if ((!pwnedBrownBagData.TryGetValue(brownBagItemKey, out string brownBagValue)
                            || DateTime.Parse(brownBagValue, CultureInfo.InvariantCulture) < breachDate)
                            && !string.IsNullOrWhiteSpace(breachTitle))
                        {
                            var breachSummary = new Breach
                            {
                                Title = breachTitle,
                                EmailAddress = emailAddress.Copy(),
                                BrownBagItemKey = brownBagItemKey,
                                BreachDate = breachDate
                            };

                            breachesSummary.Add(breachSummary);

                            await _dataManager.SetBrownBagDataAsync(
                                breachSummary.BrownBagItemKey,
                                breachSummary.BreachDate.ToString(CultureInfo.InvariantCulture),
                                cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
            } while (locationStart > -1);

            return breachesSummary;
        }
    }
}
