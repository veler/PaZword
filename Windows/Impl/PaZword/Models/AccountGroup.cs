using PaZword.Api.Collections;
using PaZword.Api.Models;
using PaZword.Core;
using System.Collections.Generic;

namespace PaZword.Models
{
    /// <summary>
    /// Represents a group of account when unzomming in the list of accounts in the UI.
    /// </summary>
    public sealed class AccountGroup
    {
        /// <summary>
        /// Gets the list of accounts associated to the group.
        /// </summary>
        public ConcurrentObservableCollection<Account> Accounts { get; }

        public string GroupName { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="AccountGroup"/> class.
        /// </summary>
        /// <param name="groupName">the name of the group.</param>
        public AccountGroup(string groupName)
        {
            GroupName = Arguments.NotNullOrWhiteSpace(groupName, nameof(groupName));
            Accounts = new ConcurrentObservableCollection<Account>();
        }

        public override bool Equals(object obj)
        {
            return obj.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            var hashCode = -592623897;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GroupName);
            hashCode = hashCode * -1521134295 + EqualityComparer<ConcurrentObservableCollection<Account>>.Default.GetHashCode(Accounts);
            return hashCode;
        }
    }
}
