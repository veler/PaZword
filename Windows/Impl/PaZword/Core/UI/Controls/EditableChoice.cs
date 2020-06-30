using PaZword.Api.UI.Controls;
using PaZword.Localization;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditableChoice"/>
    /// </summary>
    public class EditableChoice : ComboBox, IEditableControl
    {
        private const string CopyButton = "CopyButton";
        private const string NormalState = "Normal";
        private const string PointerOverState = "PointerOver";

        private bool _layoutUpdatedHandled;

        protected static readonly DependencyProperty StringsProperty = DependencyProperty.Register("Strings", typeof(CoreStrings), typeof(EditableChoice), new PropertyMetadata(LanguageManager.Instance.Core));

        public static readonly DependencyProperty SelectedIndexEditingProperty = DependencyProperty.Register(nameof(SelectedIndexEditing), typeof(int), typeof(EditableChoice), new PropertyMetadata(-1));

        /// <summary>
        /// Gets or sets the selected index in editing mode.
        /// </summary>
        public int SelectedIndexEditing
        {
            get { return (int)GetValue(SelectedIndexEditingProperty); }
            set { SetValue(SelectedIndexEditingProperty, value); }
        }

        public static readonly DependencyProperty IsCopiableProperty = DependencyProperty.Register(nameof(IsCopiable), typeof(bool), typeof(EditableChoice), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableChoice"/>'s text is copiable.
        /// </summary>
        public bool IsCopiable
        {
            get { return (bool)GetValue(IsCopiableProperty); }
            set { SetValue(IsCopiableProperty, value); }
        }

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableChoice), new PropertyMetadata(false));


        /// <summary>
        /// Gets or sets a value that defines whether the <see cref="EditableChoice"/>'s text is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        public event EventHandler<ValueCopiedEventArgs> ValueCopied;

        /// <summary>
        /// Initialize a new instance of the <see cref="EditableChoice"/> class.
        /// </summary>
        public EditableChoice()
        {
            DefaultStyleKey = typeof(EditableChoice);

            SelectedIndex = -1;
            SelectedIndexEditing = -1;
            PointerEntered += EditableChoice_PointerEntered;
            PointerExited += EditableChoice_PointerExited;
            LayoutUpdated += EditableChoice_LayoutUpdated;
        }

        private void EditableChoice_LayoutUpdated(object sender, object e)
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

        private void EditableChoice_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, NormalState, true);
        }

        private void EditableChoice_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, PointerOverState, true);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(SelectedItem?.ToString());
            Clipboard.SetContent(dataPackage);

            ValueCopied?.Invoke(this, new ValueCopiedEventArgs());
        }
    }
}
