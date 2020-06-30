using PaZword.Core;
using PaZword.Models.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.ViewModels.Data.PaymentCard
{
    /// <summary>
    /// Represents a card type item in the UI.
    /// </summary>
    public sealed class PaymentCardTypeItem : ViewModelBase
    {
        /// <summary>
        /// Gets or sets the <see cref="PaymentCardType"/>.
        /// </summary>
        public PaymentCardType PaymentCardType { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image of the item.
        /// </summary>
        public BitmapImage Image { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
