namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// Matches found by the dictionary matcher contain some additional information about the matched word.
    /// </summary>
    internal class DictionaryMatch : Match
    {
        /// <summary>
        /// The dictionary word matched
        /// </summary>
        internal string MatchedWord { get; set; }

        /// <summary>
        /// The rank of the matched word in the dictionary (i.e. 1 is most frequent, and larger numbers are less common words)
        /// </summary>
        internal int Rank { get; set; }

        /// <summary>
        /// The name of the dictionary the matched word was found in
        /// </summary>
        internal string DictionaryName { get; set; }


        /// <summary>
        /// The base entropy of the match, calculated from frequency rank
        /// </summary>
        internal double BaseEntropy { get; set; }

        /// <summary>
        /// Additional entropy for this match from the use of mixed case
        /// </summary>
        internal double UppercaseEntropy { get; set; }
    }
}
