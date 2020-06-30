using System;
using System.ComponentModel;

namespace PaZword.Api.ViewModels.Data
{
    public sealed class AccountDataProviderMetadata
    {
        /// <summary>
        /// Gets or sets in which order the account data provider should appear in the list of available provider in the "Add data" menu.
        /// </summary>
        [DefaultValue(int.MaxValue)]
        public int Order { get; set; }

        /// <summary>
        /// Gets the type of <see cref="AccountData"/> that <see cref="IAccountDataProvider.CreateAccountData"/> returns.
        /// </summary>
        public Type AccountDataType { get; set; }
    }
}
