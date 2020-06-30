using PaZword.Localization;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Dialog
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public sealed partial class InputDialog : ContentDialog
    {
        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal InputDialogStrings Strings => LanguageManager.Instance.InputDialog;

        public readonly static DependencyProperty InputValueProperty = DependencyProperty.Register(
            nameof(InputValue),
            typeof(string),
            typeof(InputDialog),
            new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the category name.
        /// </summary>
        public string InputValue
        {
            get => (string)GetValue(InputValueProperty);
            set => SetValue(InputValueProperty, value);
        }

        public readonly static DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(InputDialog),
            new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the field placeholder.
        /// </summary>
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        internal ContentDialogResult Result { get; set; }

        public InputDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InputTextBox.SelectAll();
            InputTextBox.Focus(FocusState.Keyboard);
        }

        private void ContentDialog_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContentDialogResult.Primary;
        }

        private void ContentDialog_SecondaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContentDialogResult.Secondary;
        }

        private void InputTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                Result = ContentDialogResult.Primary;
                Hide();
            }
        }
    }
}
