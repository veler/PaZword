using PaZword.Api.UI.Controls;
using PaZword.Localization;
using System;
using System.Security;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditableControlBase"/>
    /// </summary>
    public class EditableControlBase : Control, IEditableControl
    {
        private const string CopyButton = "CopyButton";
        private const string NormalState = "Normal";
        private const string PointerOverState = "PointerOver";

        private bool _layoutUpdatedHandled;

        protected static readonly DependencyProperty StringsProperty = DependencyProperty.Register("Strings", typeof(CoreStrings), typeof(EditableControlBase), new PropertyMetadata(LanguageManager.Instance.Core));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(SecureString), typeof(EditableControlBase), new PropertyMetadata(null, TextPropertyChangedCallback));

        /// <summary>
        /// Gets or sets the text to display.
        /// </summary>
        public SecureString Text
        {
            get { return (SecureString)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextEditingProperty = DependencyProperty.Register(nameof(TextEditing), typeof(SecureString), typeof(EditableControlBase), new PropertyMetadata(null, TextEditingPropertyChangedCallback));

        /// <summary>
        /// Gets or sets the text to edit.
        /// </summary>
        public SecureString TextEditing
        {
            get { return (SecureString)GetValue(TextEditingProperty); }
            set { SetValue(TextEditingProperty, value); }
        }

        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(EditableControlBase), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the place holder text.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        public static readonly DependencyProperty IsCopiableProperty = DependencyProperty.Register(nameof(IsCopiable), typeof(bool), typeof(EditableControlBase), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableTextBlock"/>'s text is copiable.
        /// </summary>
        public bool IsCopiable
        {
            get { return (bool)GetValue(IsCopiableProperty); }
            set { SetValue(IsCopiableProperty, value); }
        }

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableControlBase), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableTextBlock"/>'s text is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        protected static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(EditableControlBase), new PropertyMetadata(true));

        /// <summary>
        /// Gets a value that defines whether text value is empty or not.
        /// </summary>
        public bool IsEmpty
        {
            get { return (bool)GetValue(IsEmptyProperty); }
            set { SetValue(IsEmptyProperty, value); }
        }

        public event EventHandler<ValueCopiedEventArgs> ValueCopied;

        /// <summary>
        /// Initialize a new instance of the <see cref="EditableControlBase"/> class.
        /// </summary>
        public EditableControlBase()
        {
            DefaultStyleKey = typeof(EditableControlBase);

            PointerEntered += EditableControlBase_PointerEntered;
            PointerExited += EditableControlBase_PointerExited;
            LayoutUpdated += EditableControlBase_LayoutUpdated;
        }

        private void EditableControlBase_LayoutUpdated(object sender, object e)
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

        private void EditableControlBase_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, NormalState, true);
        }

        private void EditableControlBase_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, PointerOverState, true);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(GetTextForClipboard());
            Clipboard.SetContent(dataPackage);

            ValueCopied?.Invoke(this, new ValueCopiedEventArgs());
        }

        private static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editableBaseControl = (EditableControlBase)d;
            editableBaseControl.TextPropertyChanged();
        }

        protected virtual string GetTextForClipboard()
        {
            return Text.ToUnsecureString();
        }

        protected virtual void TextPropertyChanged()
        {
            IsEmpty = StringExtensions.IsNullOrEmptySecureString(Text);
        }

        private static void TextEditingPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editableBaseControl = (EditableControlBase)d;
            editableBaseControl.TextEditingPropertyChanged();
        }

        protected virtual void TextEditingPropertyChanged()
        {
            IsEmpty = StringExtensions.IsNullOrEmptySecureString(TextEditing);
        }
    }
}
