using System;
using System.Linq;
using System.Security;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditablePassword"/>
    /// </summary>
    public class EditablePassword : EditableControlBase, IDisposable
    {
        private const string RevealButton = "RevealButton";
        private const string PasswordBox = "PasswordBox";

        private bool _layoutUpdatedHandled;
        private bool _isDisposed;

        public static readonly DependencyProperty DisplayStrengthProperty = DependencyProperty.Register(nameof(DisplayStrength), typeof(bool), typeof(EditablePassword), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that defines whether the strength indicator must be displayed.
        /// </summary>
        public bool DisplayStrength
        {
            get { return (bool)GetValue(DisplayStrengthProperty); }
            set { SetValue(DisplayStrengthProperty, value); }
        }

        protected static readonly DependencyProperty DisplayedTextProperty = DependencyProperty.Register(nameof(DisplayedText), typeof(SecureString), typeof(EditablePassword), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text to display. It can be a clean text or a string with password character.
        /// </summary>
        protected SecureString DisplayedText
        {
            get { return (SecureString)GetValue(DisplayedTextProperty); }
            set { SetValue(DisplayedTextProperty, value); }
        }

        protected static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(nameof(Strength), typeof(int), typeof(EditablePassword), new PropertyMetadata(0));

        /// <summary>
        /// Gets or sets the percent of strength of the password.
        /// </summary>
        public int Strength
        {
            get { return (int)GetValue(StrengthProperty); }
            protected set { SetValue(StrengthProperty, value); }
        }

        protected static readonly DependencyProperty StrengthBrushProperty = DependencyProperty.Register(nameof(StrengthBrush), typeof(Brush), typeof(EditablePassword), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the color brush associated to the percent of strength of the password.
        /// </summary>
        protected Brush StrengthBrush
        {
            get { return (Brush)GetValue(StrengthBrushProperty); }
            set { SetValue(StrengthBrushProperty, value); }
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="EditablePassword"/> class.
        /// </summary>
        public EditablePassword()
        {
            DefaultStyleKey = typeof(EditablePassword);

            LayoutUpdated += EditablePassword_LayoutUpdated;
        }

        ~EditablePassword()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisplayedText?.Dispose();
            }

            _isDisposed = true;
        }

        private void EditablePassword_LayoutUpdated(object sender, object e)
        {
            if (_layoutUpdatedHandled)
            {
                return;
            }

            var revealButton = (Button)GetTemplateChild(RevealButton);
            if (revealButton != null)
            {
                revealButton.AddHandler(PointerPressedEvent, new PointerEventHandler(RevealButton_PointerPressed), true);
                revealButton.AddHandler(PointerReleasedEvent, new PointerEventHandler(RevealButton_PointerReleased), true);

                TextPropertyChanged();
                _layoutUpdatedHandled = true;
            }

        }

        private void RevealButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DisplayedText = Text;
        }

        private void RevealButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            TextPropertyChanged();
        }

        protected override void TextPropertyChanged()
        {
            base.TextPropertyChanged();

            if (Text.Length < 150)
            {
                (SolidColorBrush colorBrush, int strength) result = CoreHelper.DeterminePasswordStrength(Text.ToUnsecureString());
                StrengthBrush = result.colorBrush;
                Strength = result.strength;
            }

            var passwordBox = (PasswordBox)GetTemplateChild(PasswordBox);

            if (passwordBox == null)
            {
                return;
            }

            var length = Text.Length;
            var chars = new char[length];
            Array.Fill(chars, passwordBox.PasswordChar.ToCharArray().FirstOrDefault());
            DisplayedText = new string(chars).ToSecureString();
        }

        protected override void TextEditingPropertyChanged()
        {
            base.TextEditingPropertyChanged();

            if (TextEditing.Length < 150)
            {
                (SolidColorBrush colorBrush, int strength) result = CoreHelper.DeterminePasswordStrength(TextEditing.ToUnsecureString());
                StrengthBrush = result.colorBrush;
                Strength = result.strength;
            }
        }
    }
}
