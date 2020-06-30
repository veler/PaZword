using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PaZword.ViewModels.Dialog
{
    /// <summary>
    /// Interaction logic for <see cref="PasswordGeneratorDialog"/>
    /// </summary>
    [Export(typeof(PasswordGeneratorDialogViewModel))]
    public sealed class PasswordGeneratorDialogViewModel : ViewModelBase
    {
        private const string PrimaryButtonEvent = "PasswordGenerator.PrimaryButton.Command";
        private const string SecondaryButtonEvent = "PasswordGenerator.SecondaryButton.Command";
        private const string RefreshPasswordEvent = "PasswordGenerator.RefreshPassword.Command";

        private readonly char[] PasswordCharacters = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+={[}]|\:;<>/?.,".ToCharArray();
        private readonly Dictionary<char, List<char>> _lettersReplacement = new Dictionary<char, List<char>>
        {
            ['a'] = new List<char> { '@', 'a' },
            ['e'] = new List<char> { '3', 'e' },
            ['i'] = new List<char> { '|', 'i', '1' },
            ['o'] = new List<char> { '0', 'o', '*' },
            ['A'] = new List<char> { 'A' },
            ['E'] = new List<char> { '3', 'e', 'E' },
            ['I'] = new List<char> { 'l', '!', '1' },
            ['O'] = new List<char> { '0', 'O', 'o', '*' }
        };

        private readonly Random _random = new Random();
        private readonly ILogger _logger;
        private readonly ISerializationProvider _serializationProvider;
        private readonly ISettingsProvider _settingsProvider;

        private SecureString _generatedPassword;
        private SolidColorBrush _strengthBrush;
        private int _strength;
        private bool _easyToRead;
        private bool _fetchingDictionary;
        private string _fetchingDictionaryDescription;
        private IReadOnlyList<string> _wordDictionary;

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal PasswordGeneratorDialogStrings Strings => LanguageManager.Instance.PasswordGeneratorDialog;

        /// <summary>
        /// Gets or sets the generated password.
        /// </summary>
        internal SecureString GeneratedPassword
        {
            get => _generatedPassword;
            private set
            {
                _generatedPassword = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the color brush representing the strength of the password.
        /// </summary>
        internal SolidColorBrush StrengthBrush
        {
            get => _strengthBrush;
            private set
            {
                _strengthBrush = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the strength of the password.
        /// </summary>
        internal int Strength
        {
            get => _strength;
            private set
            {
                _strength = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the desired length of the password.
        /// </summary>
        internal int Length
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.PasswordGeneratorLength);
            set
            {
                _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorLength, value);
                RaisePropertyChanged();
                RefreshPasswordCommand.Execute(null);
            }
        }

        /// <summary>
        /// Gets or sets whether the password should be easy to read and remember or not.
        /// </summary>
        internal bool EasyToRead
        {
            get => _easyToRead;
            set
            {
                _easyToRead = value;
                RaisePropertyChanged();

                if (value)
                {
                    FetchDictionaryAndGeneratePasswordAsync().ForgetSafely();
                }
                else
                {
                    FetchingDictionaryDescription = string.Empty;
                    FetchingDictionary = false;
                    _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead, false);
                    RefreshPasswordCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the app is downloading a dictionary of words.
        /// </summary>
        internal bool FetchingDictionary
        {
            get => _fetchingDictionary;
            private set
            {
                _fetchingDictionary = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the description to show during or after fetching.
        /// </summary>
        internal string FetchingDictionaryDescription
        {
            get => _fetchingDictionaryDescription;
            set
            {
                _fetchingDictionaryDescription = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Raised when the dialog should close.
        /// </summary>
        internal event EventHandler<ContentDialogResult> CloseDialog;

        [ImportingConstructor]
        public PasswordGeneratorDialogViewModel(
            ILogger logger,
            ISerializationProvider serializationProvider,
            ISettingsProvider settingsProvider)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));

            PrimaryButtonClickCommand = new ActionCommand<object>(_logger, PrimaryButtonEvent, ExecutePrimaryButtonClickCommand);
            SecondaryButtonClickCommand = new ActionCommand<object>(_logger, SecondaryButtonEvent, ExecuteSecondaryButtonClickCommand);
            RefreshPasswordCommand = new AsyncActionCommand<object>(_logger, RefreshPasswordEvent, ExecuteRefreshPasswordCommandAsync);

            if (_settingsProvider.GetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead))
            {
                // this will load the dictionary and generate a password.
                EasyToRead = true;
            }
            else
            {
                RefreshPasswordCommand.Execute(null);
            }
        }

        #region PrimaryButtonClickCommand

        internal ActionCommand<object> PrimaryButtonClickCommand { get; }

        private void ExecutePrimaryButtonClickCommand(object parameter)
        {
            CloseDialog?.Invoke(this, ContentDialogResult.Primary);
        }

        #endregion

        #region SecondaryButtonClickCommand

        internal ActionCommand<object> SecondaryButtonClickCommand { get; }

        private void ExecuteSecondaryButtonClickCommand(object parameter)
        {
            CloseDialog?.Invoke(this, ContentDialogResult.Secondary);
        }

        #endregion

        #region RefreshPasswordCommand

        internal AsyncActionCommand<object> RefreshPasswordCommand { get; }

        private async Task ExecuteRefreshPasswordCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            string password = GenerateNewPassword();

            GeneratedPassword = password.ToSecureString();

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                (SolidColorBrush colorBrush, int strength) = CoreHelper.DeterminePasswordStrength(password);
                StrengthBrush = colorBrush;
                Strength = strength;
            }).ConfigureAwait(false);
        }

        #endregion

        private async Task FetchDictionaryAndGeneratePasswordAsync()
        {
            FetchingDictionary = true;
            FetchingDictionaryDescription = Strings.FetchingDictionary;

            try
            {
                var json = string.Empty;
                var fileName = Path.GetFileName(Strings.WordDictionaryUrl);
                IStorageItem file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);

                if (file == null)
                {
                    if (!CoreHelper.IsInternetAccess())
                    {
                        FetchingDictionaryDescription = Strings.NoInternet;
                        EasyToRead = false;
                        _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead, false);
                        return;
                    }

                    using (var httpClient = new HttpClient())
                    using (HttpResponseMessage responseMessage = await httpClient.GetAsync(new Uri(Strings.WordDictionaryUrl)).ConfigureAwait(false))
                    {
                        if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            FetchingDictionaryDescription = Strings.ErrorOccured;
                            EasyToRead = false;
                            _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead, false);
                            return;
                        }

                        json = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                        await CoreHelper.RetryAsync(async () =>
                        {
                            // Create and save the dictionary on the local hard drive so we can reuse it.
                            StorageFile dataFileCreated = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(dataFileCreated, json);
                        }).ConfigureAwait(false);
                    }
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    StorageFile dictionaryFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                    if (!dictionaryFile.IsAvailable)
                    {
                        throw new FileLoadException("The file isn't available.");
                    }

                    json = await FileIO.ReadTextAsync(dictionaryFile);
                }

                _wordDictionary = _serializationProvider.DeserializeObject<IReadOnlyList<string>>(json);

                FetchingDictionaryDescription = string.Empty;
                _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead, true);
                RefreshPasswordCommand.Execute(null);
            }
            catch
            {
                // TODO: log that
                FetchingDictionaryDescription = Strings.ErrorOccured;
                EasyToRead = false;
                _settingsProvider.SetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead, false);
            }
            finally
            {
                FetchingDictionary = false;
            }
        }

        private string GenerateNewPassword()
        {
            if (_settingsProvider.GetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead))
            {
                return GenerateEasyToReadPassword();
            }

            return GenerateRandomPassword(Length);
        }

        private string GenerateEasyToReadPassword()
        {
            int i;
            var words = new List<string>();

            // Takes 20 random words max.
            for (i = 0; i < 20; i++)
            {
                var wordId = _random.Next(0, _wordDictionary.Count);
                words.Add(_wordDictionary[wordId]);
            }

            // Filter and shuffle the list.
            words = words
                .Distinct()
                .Where(w => w.Length > 2)
                .OrderBy(w => Guid.NewGuid())
                .ToList();

            // Takes the closest word from the desired length, or takes 2 words if it fits in the length.
            var desiredLength = Length;
            i = 0;
            var selectedWord = string.Empty;
            while (i < words.Count)
            {
                string word = words[i];
                if (selectedWord.Length < word.Length && word.Length < desiredLength)
                {
                    selectedWord = char.ToUpper(word[0], CultureInfo.CurrentCulture) + word.Substring(1);
                }
                i++;
            }

            string additionalWord = words.Where(
                word => !string.Equals(selectedWord, word, StringComparison.Ordinal)
                        && word.Length < desiredLength - selectedWord.Length)
                .OrderBy(w => Guid.NewGuid())
                .FirstOrDefault();

            if (additionalWord != null && additionalWord.Length > 0)
            {
                selectedWord += char.ToUpper(additionalWord[0], CultureInfo.CurrentCulture) + additionalWord.Substring(1);
            }

            // Randomize vowels.
            for (i = 0; i < selectedWord.Length; i++)
            {
                char c = selectedWord[i];
                if (_lettersReplacement.TryGetValue(c, out List<char> replacementChars))
                {
                    var charId = _random.Next(0, replacementChars.Count);
                    selectedWord = selectedWord.Remove(i, 1);
                    selectedWord = selectedWord.Insert(i, replacementChars[charId].ToString());
                }
            }

            // Complete the password with a prefix and suffix if necessary to match the desired length.
            var remainingLength = desiredLength - selectedWord.Length;

            var prefixPasswordLength = (int)Math.Ceiling(remainingLength / 2.0);
            var suffixPasswordLength = (int)Math.Floor(remainingLength / 2.0);

            string prefixPassword = GenerateRandomPassword(prefixPasswordLength);
            string suffixPassword = GenerateRandomPassword(suffixPasswordLength);

            return prefixPassword + selectedWord + suffixPassword;
        }

        private string GenerateRandomPassword(int length)
        {
            if (length == 0)
            {
                return string.Empty;
            }

            var password = new StringBuilder();
            var buffer = new byte[4 * length];

            // Generating random numbers cryptographically is better in this scenario than a regular Random
            // since it's better for determining encryption keys.
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(buffer);
            }

            for (int i = 0; i < length; i++)
            {
                uint random = BitConverter.ToUInt32(buffer, i * 4);
                long charToPickUp = random % PasswordCharacters.Length;
                char c = PasswordCharacters[charToPickUp];

                password.Append(c);
            }

            return password.ToString();
        }
    }
}
