using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// <para>Use a regular expression to match agains the password. (e.g. 'year' and 'digits' pattern matchers are implemented with this matcher.</para>
    /// <para>A note about cardinality: the cardinality parameter is used to calculate the entropy of matches found with the regex matcher. Since
    /// this cannot be calculated automatically from the regex pattern it must be provided. It can be provided per-character or per-match. Per-match will
    /// result in every match having the same entropy (lg cardinality) whereas per-character will depend on the match length (lg cardinality ^ length)</para>
    /// </summary>
    internal sealed class RegexMatcher : IMatcher
    {
        readonly Regex matchRegex;
        readonly string matcherName;
        readonly int cardinality;
        readonly bool perCharCardinality;

        /// <summary>
        /// Create a new regex pattern matcher
        /// </summary>
        /// <param name="pattern">The regex pattern to match</param>
        /// <param name="cardinality">The cardinality of this match. Since this cannot be calculated from a pattern it must be provided. Can
        /// be give per-matched-character or per-match</param>
        /// <param name="perCharCardinality">True if cardinality is given as per-matched-character</param>
        /// <param name="matcherName">The name to give this matcher ('pattern' in resulting matches)</param>
        internal RegexMatcher(string pattern, int cardinality, bool perCharCardinality = true, string matcherName = "regex")
            : this(new Regex(pattern), cardinality, perCharCardinality, matcherName)
        {
        }

        /// <summary>
        /// Create a new regex pattern matcher
        /// </summary>
        /// <param name="matchRegex">The regex object used to perform matching</param>
        /// <param name="cardinality">The cardinality of this match. Since this cannot be calculated from a pattern it must be provided. Can
        /// be give per-matched-character or per-match</param>
        /// <param name="perCharCardinality">True if cardinality is given as per-matched-character</param>
        /// <param name="matcherName">The name to give this matcher ('pattern' in resulting matches)</param>
        internal RegexMatcher(Regex matchRegex, int cardinality, bool perCharCardinality, string matcherName = "regex")
        {
            this.matchRegex = matchRegex;
            this.matcherName = matcherName;
            this.cardinality = cardinality;
            this.perCharCardinality = perCharCardinality;
        }

        /// <summary>
        /// Find all matches of the regex in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>An enumerable of matches for each regex match in <paramref name="password"/></returns>
        public IEnumerable<Match> MatchPassword(string password)
        {
            var reMatches = matchRegex.Matches(password);

            var pwMatches = new List<Match>();

            foreach (System.Text.RegularExpressions.Match rem in reMatches)
            {
                pwMatches.Add(new Match()
                {
                    Pattern = matcherName,
                    i = rem.Index,
                    j = rem.Index + rem.Length - 1,
                    Token = password.Substring(rem.Index, rem.Length),
                    Cardinality = cardinality,
                    Entropy = Math.Log((perCharCardinality ? Math.Pow(cardinality, rem.Length) : cardinality), 2) // Raise cardinality to length when giver per character
                });
            }

            return pwMatches;
        }
    }
}
