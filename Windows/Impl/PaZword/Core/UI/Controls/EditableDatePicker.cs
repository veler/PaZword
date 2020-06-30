using PaZword.Api.UI.Controls;
using PaZword.Localization;
using System;
using System.Globalization;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditableDatePicker"/>
    /// </summary>
    public class EditableDatePicker : DatePicker, IEditableControl
    {
        private const string CopyButton = "CopyButton";
        private const string NormalState = "Normal";
        private const string PointerOverState = "PointerOver";

        private bool _layoutUpdatedHandled;

        protected static readonly DependencyProperty StringsProperty = DependencyProperty.Register("Strings", typeof(CoreStrings), typeof(EditableDatePicker), new PropertyMetadata(LanguageManager.Instance.Core));

        public static readonly DependencyProperty DateEditingProperty = DependencyProperty.Register(nameof(DateEditing), typeof(DateTimeOffset), typeof(EditableDatePicker), new PropertyMetadata(default(DateTimeOffset)));

        /// <summary>
        /// Gets or sets the selected date time in editing mode.
        /// </summary>
        public DateTimeOffset DateEditing
        {
            get { return (DateTimeOffset)GetValue(DateEditingProperty); }
            set { SetValue(DateEditingProperty, value); }
        }

        public static readonly DependencyProperty ToStringFormatProperty = DependencyProperty.Register(nameof(ToStringFormat), typeof(string), typeof(EditableDatePicker), new PropertyMetadata("R"));

        /// <summary>
        /// Gets or sets the format of date to generates when copying the data to the clipboard. See also https://msdn.microsoft.com/en-us/library/bb346136(v=vs.110).aspx.
        /// </summary>
        public string ToStringFormat
        {
            get { return (string)GetValue(ToStringFormatProperty); }
            set { SetValue(ToStringFormatProperty, value); }
        }

        public static readonly DependencyProperty IsCopiableProperty = DependencyProperty.Register(nameof(IsCopiable), typeof(bool), typeof(EditableDatePicker), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableDatePicker"/>'s text is copiable.
        /// </summary>
        public bool IsCopiable
        {
            get { return (bool)GetValue(IsCopiableProperty); }
            set { SetValue(IsCopiableProperty, value); }
        }

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableDatePicker), new PropertyMetadata(false));


        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableDatePicker"/>'s text is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        public event EventHandler<ValueCopiedEventArgs> ValueCopied;

        /// <summary>
        /// Initialize a new instance of the <see cref="EditableDatePicker"/> class.
        /// </summary>
        public EditableDatePicker()
        {
            DefaultStyleKey = typeof(EditableDatePicker);

            PointerEntered += EditableDatePicker_PointerEntered;
            PointerExited += EditableDatePicker_PointerExited;
            LayoutUpdated += EditableDatePicker_LayoutUpdated;
        }

        private void EditableDatePicker_LayoutUpdated(object sender, object e)
        {
            if (_layoutUpdatedHandled)
            {
                return;
            }

            var copyButton = (Button)GetTemplateChild(CopyButton);
            if (copyButton != null)
            {
                copyButton.Click += CopyButton_Click;
                _layoutUpdatedHandled = true;
            }
        }

        private void EditableDatePicker_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, NormalState, true);
        }

        private void EditableDatePicker_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, PointerOverState, true);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            string value = Date.ToString(ToStringFormat, CultureInfo.CurrentCulture);

            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(value);
            Clipboard.SetContent(dataPackage);

            ValueCopied?.Invoke(this, new ValueCopiedEventArgs());
        }
    }
}
