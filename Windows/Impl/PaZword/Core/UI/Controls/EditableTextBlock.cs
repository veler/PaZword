using Windows.UI.Xaml;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditableTextBlock"/>
    /// </summary>
    public sealed class EditableTextBlock : EditableControlBase
    {
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(EditableTextBlock), new PropertyMetadata(TextWrapping.NoWrap));

        /// <summary>
        /// Gets or sets a value that defines how text is wrapping when it overflows the edge of its containing box.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(nameof(TextTrimming), typeof(TextTrimming), typeof(EditableTextBlock), new PropertyMetadata(TextTrimming.CharacterEllipsis));

        /// <summary>
        /// Gets or sets a value that defines how text is trimmed when it overflows the edge of its containing box.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public static readonly DependencyProperty LineStackingStrategyProperty = DependencyProperty.Register(nameof(LineStackingStrategy), typeof(LineStackingStrategy), typeof(EditableTextBlock), new PropertyMetadata(default(LineStackingStrategy)));

        /// <summary>
        /// Gets or sets a value that indicates how a line box is determined for each line of text in the TextBlock.
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        public static readonly DependencyProperty MaxLinesProperty = DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(EditableTextBlock), new PropertyMetadata(0));

        /// <summary>
        /// Gets or sets the maximum lines of text shown in the TextBlock.
        /// </summary>
        public int MaxLines
        {
            get { return (int)GetValue(MaxLinesProperty); }
            set { SetValue(MaxLinesProperty, value); }
        }

        public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(EditableTextBlock), new PropertyMetadata(0.0));

        /// <summary>
        /// Gets or sets the height of each line of content.
        /// </summary>
        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(nameof(Mask), typeof(string), typeof(EditableTextBlock), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets a mask to restrict the text box input to a specific format.
        /// </summary>
        public string Mask
        {
            get { return (string)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        public static readonly DependencyProperty MaskPlaceholderProperty = DependencyProperty.Register(nameof(MaskPlaceholder), typeof(string), typeof(EditableTextBlock), new PropertyMetadata("_"));

        /// <summary>
        /// Gets or sets a mask placeholder.
        /// </summary>
        public string MaskPlaceholder
        {
            get { return (string)GetValue(MaskPlaceholderProperty); }
            set { SetValue(MaskPlaceholderProperty, value); }
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="EditableTextBlock"/> class.
        /// </summary>
        public EditableTextBlock()
        {
            DefaultStyleKey = typeof(EditableTextBlock);
        }

        protected override string GetTextForClipboard()
        {
            string text = base.GetTextForClipboard();

            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Mask) && Mask.Contains("-", System.StringComparison.Ordinal))
            {
                text = text.Replace("-", string.Empty, System.StringComparison.Ordinal);
            }

            return text;
        }
    }
}
