using PaZword.Api.Models;
using System;

namespace PaZword.Api.ViewModels.Data
{
    /// <summary>
    /// Provides a set of methods and properties that represents and create and manage an <see cref="AccountData"/>.
    /// </summary>
    public interface IAccountDataProvider
    {
        /// <summary>
        /// Gets the name of the account data to show in the user interface.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Generates a new instance of <see cref="IAccountDataViewModel"/>.
        /// </summary>
        /// <param name="account">The <see cref="AccountData"/> for which we create this <see cref="IAccountDataViewModel"/>.</param>
        /// <returns>Returns a new instance of <see cref="IAccountDataViewModel"/>.</returns>
        IAccountDataViewModel CreateViewModel(AccountData account);

        /// <summary>
        /// Determines whether <see cref="CreateAccountData"/> can be called.
        /// </summary>
        /// <param name="account">The current edited account.</param>
        /// <returns>When returning <code>False</code>, the menu item in the user interface will be disabled.</returns>
        bool CanCreateAccountData(Account account);

        /// <summary>
        /// Creates a new instance of a <see cref="AccountData"/>.
        /// </summary>
        /// <param name="accountDataId">The <see cref="AccountData.Id"/> to give.</param>
        /// <returns>Returns a new instance of a <see cref="AccountData"/>.</returns>
        AccountData CreateAccountData(Guid accountDataId);
    }
}
