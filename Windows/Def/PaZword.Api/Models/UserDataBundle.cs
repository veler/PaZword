using Newtonsoft.Json;
using PaZword.Api.Collections;
using System.Collections.Concurrent;

namespace PaZword.Api.Models
{
    /// <summary>
    /// Represents a data that contains the categories and accounts.
    /// </summary>
    public sealed class UserDataBundle
    {
        #region Properties

        /// <summary>
        /// Gets or sets the accounts.
        /// </summary>
        [JsonProperty]
        public ConcurrentObservableCollection<Account> Accounts { get; }

        /// <summary>
        /// Gets or sets the categories.
        /// </summary>
        [JsonProperty]
        public ConcurrentObservableCollection<Category> Categories { get; }

        /// <summary>
        /// Gets or sets some additional informations about the user data bundle
        /// </summary>
        /// <remarks>
        /// Use this as a way to keep some settings or non-sensitive data accross several devices
        /// and linked to this <see cref="UserDataBundle"/>.
        /// Only store small data. Do NOT store sensitive data here.
        /// </remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ConcurrentDictionary<string, string> BrownBag { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new instance of the <see cref="UserDataBundle"/> class.
        /// </summary>
        public UserDataBundle()
        {
            Accounts = new ConcurrentObservableCollection<Account>();
            Categories = new ConcurrentObservableCollection<Category>();
            BrownBag = new ConcurrentDictionary<string, string>();
        }

        #endregion
    }
}
