using Newtonsoft.Json;
using PaZword.Api.Collections;
using System;

namespace PaZword.Api.Models
{
    /// <summary>
    /// Represents a basic account
    /// </summary>
    public sealed class Account : IExactEquatable<Account>
    {
        /// <summary>
        /// Gets the account's unique ID.
        /// </summary>
        [JsonProperty]
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the category id of the account.
        /// </summary>
        [JsonProperty]
        public Guid CategoryID { get; set; }

        /// <summary>
        /// Gets or sets the icon mode.
        /// </summary>
        [JsonProperty]
        public IconMode IconMode { get; set; }

        /// <summary>
        /// Gets or sets the base64 string representation of the account's icon.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Base64Icon { get; set; }

        /// <summary>
        /// Gets or sets the title of the account.
        /// </summary>
        [JsonProperty]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the url linked to the account/service.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value that defines whether the data is a favorite or not.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the data creation date.
        /// </summary>
        [JsonProperty]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the date that corresponds to the last time the account has been edited by the user.
        /// </summary>
        [JsonProperty]
        public DateTime LastModificationDate { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="AccountData"/> entered manually by the user.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ConcurrentObservableCollection<AccountData> Data { get; }

        /// <summary>
        /// Gets or sets the subtitle that match the best with the account.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AccountSubtitle { get; set; }

        /// <summary>
        /// Initialize a new instance of the <see cref="Account"/> class.
        /// </summary>
        public Account()
        {
            Data = new ConcurrentObservableCollection<AccountData>();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        public Account(Guid id)
            : this()
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            if (obj is Account account)
            {
                return Equals(account);
            }

            return false;
        }

        public bool Equals(Account other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other)
                || other.Id == Id;
        }

        public bool ExactEquals(Account other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other)
                || (other.Id == Id
                    && other.CategoryID == CategoryID
                    && other.CreationDate == CreationDate
                    && other.LastModificationDate == LastModificationDate
                    && other.IconMode == IconMode
                    && other.IsFavorite == IsFavorite
                    && string.Equals(other.Base64Icon, Base64Icon, StringComparison.Ordinal)
                    && string.Equals(other.Title, Title, StringComparison.Ordinal)
                    && string.Equals(other.Url, Url, StringComparison.Ordinal)
                    && string.Equals(other.AccountSubtitle, AccountSubtitle, StringComparison.Ordinal)
                    && other.Data.Count == Data.Count
                    && AllDataAreExactEqual(other));
        }

        private bool AllDataAreExactEqual(Account other)
        {
            if (other.Data.Count != Data.Count)
            {
                return false;
            }

            for (int i = 0; i < other.Data.Count; i++)
            {
                if (!other.Data[i].ExactEquals(Data[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ 137;
        }

        public static bool operator ==(Account left, Account right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Account left, Account right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            for (int i = 0; i < Data.Count; i++)
            {
                Data[i].Dispose();
            }
        }
    }
}
