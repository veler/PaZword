using PaZword.Core;
using PaZword.Localization;
using System;
using System.Globalization;
using System.Security;

namespace PaZword.Models.Pwned
{
    internal sealed class Breach
    {
        public string Title { get; set; }

        internal DateTime BreachDate { get; set; }

        internal SecureString EmailAddress { get; set; }

        internal string BrownBagItemKey { get; set; }

        public override string ToString()
        {
            return LanguageManager.Instance.BreachDiscoveredDialog.GetFormattedBreachInformation(
                BreachDate.ToString("y", CultureInfo.CurrentCulture),
                EmailAddress.ToUnsecureString());
        }
    }
}
