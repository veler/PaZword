using System;

namespace PaZword.Api.UI.Controls
{
    /// <summary>
    /// Provides a base definition of a UI control that can has a state editable and read-only.
    /// </summary>
    public interface IEditableControl
    {
        /// <summary>
        /// Gets or sets whether the control is read-only or in editing mode.
        /// </summary>
        bool IsEditing { get; set; }

        /// <summary>
        /// Gets or sets whether the user can copy the control's value to the clipboard.
        /// </summary>
        bool IsCopiable { get; set; }

        /// <summary>
        /// Raised when the control's value has been sent to the clipboard.
        /// </summary>
        event EventHandler<ValueCopiedEventArgs> ValueCopied;
    }
}
