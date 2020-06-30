using PaZword.Localization;
using PaZword.Models.Data;
using System;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.ViewModels.Data.PaymentCard
{
    internal static class PaymentCardTypeItems
    {
        /// <summary>
        /// American Express card definition.
        /// </summary>
        public readonly static PaymentCardTypeItem AmericanExpress = new PaymentCardTypeItem()
        {
            PaymentCardType = PaymentCardType.AmericanExpress,
            Name = LanguageManager.Instance.PaymentCardData.AmericanExpress,
            Image = new BitmapImage(new Uri("ms-appx:///Assets/PaymentsCards/amex.png", UriKind.Absolute))
        };

        /// <summary>
        /// Discovery card definition.
        /// </summary>
        public readonly static PaymentCardTypeItem Discovery = new PaymentCardTypeItem()
        {
            PaymentCardType = PaymentCardType.Discovery,
            Name = LanguageManager.Instance.PaymentCardData.Discovery,
            Image = new BitmapImage(new Uri("ms-appx:///Assets/PaymentsCards/discover.png", UriKind.Absolute))
        };

        /// <summary>
        /// Mastercard card definition.
        /// </summary>
        public readonly static PaymentCardTypeItem Mastercard = new PaymentCardTypeItem()
        {
            PaymentCardType = PaymentCardType.Mastercard,
            Name = LanguageManager.Instance.PaymentCardData.Mastercard,
            Image = new BitmapImage(new Uri("ms-appx:///Assets/PaymentsCards/mastercard.png", UriKind.Absolute))
        };

        /// <summary>
        /// Visa card definition.
        /// </summary>
        public readonly static PaymentCardTypeItem Visa = new PaymentCardTypeItem()
        {
            PaymentCardType = PaymentCardType.Visa,
            Name = LanguageManager.Instance.PaymentCardData.Visa,
            Image = new BitmapImage(new Uri("ms-appx:///Assets/PaymentsCards/visa.png", UriKind.Absolute))
        };

        /// <summary>
        /// Other payment card definition.
        /// </summary>
        public readonly static PaymentCardTypeItem Other = new PaymentCardTypeItem()
        {
            PaymentCardType = PaymentCardType.Other,
            Name = LanguageManager.Instance.PaymentCardData.Other,
            Image = new BitmapImage(new Uri("ms-appx:///Assets/PaymentsCards/other.png", UriKind.Absolute))
        };
    }
}
