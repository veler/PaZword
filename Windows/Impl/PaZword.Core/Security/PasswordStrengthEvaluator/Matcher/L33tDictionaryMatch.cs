using System.Collections.Generic;

namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// L33tMatcher results are like dictionary match results with some extra information that pertains to the extra entropy that
    /// is garnered by using substitutions.
    /// </summary>
    internal sealed class L33tDictionaryMatch : DictionaryMatch
    {
        /// <summary>
        /// The extra entropy from using l33t substitutions
        /// </summary>
        internal double L33tEntropy { get; set; }

        /// <summary>
        /// The character mappings that are in use for this match
        /// </summary>
        internal Dictionary<char, char> Subs { get; set; }

        /// <summary>
        /// Create a new l33t match from a dictionary match
        /// </summary>
        /// <param name="dm">The dictionary match to initialise the l33t match from</param>
        internal L33tDictionaryMatch(DictionaryMatch dm)
        {
            this.BaseEntropy = dm.BaseEntropy;
            this.Cardinality = dm.Cardinality;
            this.DictionaryName = dm.DictionaryName;
            this.Entropy = dm.Entropy;
            this.i = dm.i;
            this.j = dm.j;
            this.MatchedWord = dm.MatchedWord;
            this.Pattern = dm.Pattern;
            this.Rank = dm.Rank;
            this.Token = dm.Token;
            this.UppercaseEntropy = dm.UppercaseEntropy;

            Subs = new Dictionary<char, char>();
        }

        /// <summary>
        /// Create an empty l33t match
        /// </summary>
        internal L33tDictionaryMatch()
        {
            Subs = new Dictionary<char, char>();
        }
    }
}
