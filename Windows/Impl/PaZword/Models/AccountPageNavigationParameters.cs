using PaZword.Api.Models;
using PaZword.Core;

namespace PaZword.Models
{
    internal sealed class AccountPageNavigationParameters
    {
        internal Account Account { get; }

        internal bool ShouldSwitchToEditMode { get; }

        public AccountPageNavigationParameters(Account account, bool shouldSwitchToEditMode)
        {
            Account = Arguments.NotNull(account, nameof(account));
            ShouldSwitchToEditMode = shouldSwitchToEditMode;
        }
    }
}
