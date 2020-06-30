using System;
using System.Collections.Generic;
using System.Linq;

namespace PaZword.Core.Security.PasswordStrengthEvaluator
{
    /// <summary>
    /// <para>Zxcvbn is used to estimate the strength of passwords. </para>
    /// 
    /// <para>This implementation is a port of the Zxcvbn JavaScript library by Dan Wheeler:
    /// https://github.com/lowe/zxcvbn</para>
    /// 
    /// <para>To quickly evaluate a password, use the <see cref="EvaluatePasswordCrackTime"/> static function.</para>
    /// 
    /// <para>To evaluate a number of passwords, create an instance of this object and repeatedly call the <see cref="EvaluatePassword"/> function.
    /// Reusing the the Zxcvbn instance will ensure that pattern matchers will only be created once rather than being recreated for each password
    /// e=being evaluated.</para>
    /// </summary>
    internal sealed class Zxcvbn
    {
        private readonly DefaultMatcherFactory matcherFactory = new DefaultMatcherFactory();

        /// <summary>
        /// <para>A static function to match a password against the default matchers without having to create
        /// an instance of Zxcvbn yourself, with supplied user data. </para>
        /// 
        /// <para>Supplied user data will be treated as another kind of dictionary matching.</para>
        /// </summary>
        /// <param name="password">the password to test</param>
        /// <returns>An estimation of the time required to crack the given password</returns>
        internal double EvaluatePasswordCrackTime(string password)
        {
            IEnumerable<Match> matches = new List<Match>();

            foreach (var matcher in matcherFactory.GetMatchers())
            {
                matches = matches.Union(matcher.MatchPassword(password));
            }

            var result = CalculateCrackTime(password, matches);

            return result;
        }

        /// <summary>
        /// Returns a new result structure initialised with data for the lowest entropy result of all of the matches passed in, adding brute-force
        /// matches where there are no lesser entropy found pattern matches.
        /// </summary>
        /// <param name="matches">Password being evaluated</param>
        /// <param name="password">List of matches found against the password</param>
        /// <returns>An estimation of the time required to crack the given password</returns>
        private static double CalculateCrackTime(string password, IEnumerable<Match> matches)
        {
            var bruteforce_cardinality = PasswordScoring.PasswordCardinality(password);

            // Minimum entropy up to position k in the password
            var minimumEntropyToIndex = new double[password.Length];
            var bestMatchForIndex = new Match[password.Length];

            for (var k = 0; k < password.Length; k++)
            {
                // Start with bruteforce scenario added to previous sequence to beat
                minimumEntropyToIndex[k] = (k == 0 ? 0 : minimumEntropyToIndex[k - 1]) + Math.Log(bruteforce_cardinality, 2);

                // All matches that end at the current character, test to see if the entropy is less
                foreach (var match in matches.Where(m => m.j == k))
                {
                    var candidate_entropy = (match.i <= 0 ? 0 : minimumEntropyToIndex[match.i - 1]) + match.Entropy;
                    if (candidate_entropy < minimumEntropyToIndex[k])
                    {
                        minimumEntropyToIndex[k] = candidate_entropy;
                        bestMatchForIndex[k] = match;
                    }
                }
            }

            var minEntropy = (password.Length == 0 ? 0 : minimumEntropyToIndex[password.Length - 1]);
            var crackTime = PasswordScoring.EntropyToCrackTime(minEntropy);

            return Math.Round(crackTime, 3);
        }
    }
}
