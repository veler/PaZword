using PaZword.Api.Models;
using System;

namespace PaZword.Core
{
    internal sealed class SelectAccountEventArgs : EventArgs
    {
        internal Account Account { get; }

        public SelectAccountEventArgs(Account account)
        {
            Account = account;
        }
    }
}
