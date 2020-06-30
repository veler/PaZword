namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// A match found by the date matcher
    /// </summary>
    internal sealed class DateMatch : Match
    {
        /// <summary>
        /// The detected year
        /// </summary>
        internal int Year { get; set; }

        /// <summary>
        /// The detected month
        /// </summary>
        internal int Month { get; set; }

        /// <summary>
        /// The detected day
        /// </summary>
        internal int Day { get; set; }

        /// <summary>
        /// Where a date with separators is matched, this will contain the separator that was used (e.g. '/', '-')
        /// </summary>
        internal string Separator { get; set; }
    }
}
