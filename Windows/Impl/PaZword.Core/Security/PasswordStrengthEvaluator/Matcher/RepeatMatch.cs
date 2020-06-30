namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// A match found with the RepeatMatcher
    /// </summary>
    internal sealed class RepeatMatch : Match
    {
        /// <summary>
        /// The character that was repeated
        /// </summary>
        internal char RepeatChar { get; set; }
    }
}
