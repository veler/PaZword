namespace PaZword.Core.Security.PasswordStrengthEvaluator
{
    /// <summary>
    /// <para>A single match that one of the pattern matchers has made against the password being tested.</para>
    /// 
    /// <para>Some pattern matchers implement subclasses of match that can provide more information on their specific results.</para>
    /// 
    /// <para>Matches must all have the <see cref="Pattern"/>, <see cref="Token"/>, <see cref="Entropy"/>, <see cref="i"/> and
    /// <see cref="j"/> fields (i.e. all but the <see cref="Cardinality"/> field, which is optional) set before being returned from the matcher
    /// in which they are created.</para>
    /// </summary>
    internal class Match
    {
        /// <summary>
        /// The name of the pattern matcher used to generate this match
        /// </summary>
        internal string Pattern { get; set; }

        /// <summary>
        /// The portion of the password that was matched
        /// </summary>
        internal string Token { get; set; }

        /// <summary>
        /// The entropy that this portion of the password covers using the current pattern matching technique
        /// </summary>
        internal double Entropy { get; set; }


        // The following are more internal measures, but may be useful to consumers

        /// <summary>
        /// Some pattern matchers can associate the cardinality of the set of possible matches that the 
        /// entropy calculation is derived from. Not all matchers provide a value for cardinality.
        /// </summary>
        internal int Cardinality { get; set; }

        /// <summary>
        /// The start index in the password string of the matched token. 
        /// </summary>
        internal int i { get; set; } // Start Index

        /// <summary>
        /// The end index in the password string of the matched token.
        /// </summary>
        internal int j { get; set; } // End Index
    }
}
