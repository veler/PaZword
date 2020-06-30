namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// A match made using the <see cref="SequenceMatcher"/> containing some additional sequence information.
    /// </summary>
    internal sealed class SequenceMatch : Match
    {
        /// <summary>
        /// The name of the sequence that the match was found in (e.g. 'lower', 'upper', 'digits')
        /// </summary>
        internal string SequenceName { get; set; }

        /// <summary>
        /// The size of the sequence the match was found in (e.g. 26 for lowercase letters)
        /// </summary>
        internal int SequenceSize { get; set; }

        /// <summary>
        /// Whether the match was found in ascending order (cdefg) or not (zyxw)
        /// </summary>
        internal bool Ascending { get; set; }
    }
}
