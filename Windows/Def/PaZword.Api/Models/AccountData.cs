using Newtonsoft.Json;
using PaZword.Api.Security;
using System;
using Windows.UI.Xaml;

namespace PaZword.Api.Models
{
    /// <summary>
    /// Represents a data for an account.
    /// </summary>
    public abstract class AccountData : IExactEquatable<AccountData>, IDisposable
    {
        [JsonIgnore]
        protected bool IsDisposed { get; private set; }

        [JsonIgnore]
        protected IEncryptionProvider EncryptionProvider { get; }

        /// <summary>
        /// Gets the account data's unique ID.
        /// </summary>
        [JsonProperty]
        public Guid Id { get; private set; }

        /// <summary>
        /// Initialize a new instance of the <see cref="AccountData"/> class.
        /// </summary>
        public AccountData()
        {
            if (Application.Current is IApp app)
            {
                EncryptionProvider = app.ExportProvider.GetExport<IEncryptionProvider>();
            }
            else
            {
                throw new ApplicationException($"Unable to convert Application to {nameof(IApp)}");
            }
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="AccountData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account data</param>
        public AccountData(Guid id)
            : this()
        {
            Id = id;
        }

        ~AccountData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        public override bool Equals(object obj)
        {
            if (obj is AccountData account)
            {
                return Equals(account);
            }

            return false;
        }

        public bool Equals(AccountData other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other)
                || other.Id == Id;
        }

        public abstract bool ExactEquals(AccountData other);

        public static bool operator ==(AccountData left, AccountData right)
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
        public static bool operator !=(AccountData left, AccountData right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ 137;
        }
    }
}
