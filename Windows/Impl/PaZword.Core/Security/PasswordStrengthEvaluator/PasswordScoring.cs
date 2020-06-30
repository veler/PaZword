using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PaZword.Core.Security.PasswordStrengthEvaluator
{
    /// <summary>
    /// Some useful shared functions used for evaluating passwords
    /// </summary>
    internal static class PasswordScoring
    {
        /// <summary>
        /// Calculate the cardinality of the minimal character sets necessary to brute force the password (roughly)
        /// (e.g. lowercase = 26, numbers = 10, lowercase + numbers = 36)
        /// </summary>
        /// <param name="password">THe password to evaluate</param>
        /// <returns>An estimation of the cardinality of charactes for this password</returns>
        internal static int PasswordCardinality(string password)
        {
            var cl = 0;

            if (password.Any(c => 'a' <= c && c <= 'z')) cl += 26; // Lowercase
            if (password.Any(c => 'A' <= c && c <= 'Z')) cl += 26; // Uppercase
            if (password.Any(c => '0' <= c && c <= '9')) cl += 10; // Numbers
            if (password.Any(c => c <= '/' || (':' <= c && c <= '@') || ('[' <= c && c <= '`') || ('{' <= c && c <= 0x7F))) cl += 33; // Symbols
            if (password.Any(c => c > 0x7F)) cl += 100; // 'Unicode' (why 100?)

            return cl;
        }

        /// <summary>
        /// Calculate a rough estimate of crack time for entropy, see zxcbn scoring.coffee for more information on the model used
        /// </summary>
        /// <param name="entropy">Entropy of password</param>
        /// <returns>An estimation of seconts taken to crack password</returns>
        internal static double EntropyToCrackTime(double entropy)
        {
            const double SingleGuess = 0.01;
            const double NumAttackers = 100;
            const double SecondsPerGuess = SingleGuess / NumAttackers;

            return 0.5 * Math.Pow(2, entropy) * SecondsPerGuess;
        }

        /// <summary>
        /// Caclulate binomial coefficient (i.e. nCk)
        /// Uses same algorithm as zxcvbn (cf. scoring.coffee), from http://blog.plover.com/math/choose.html 
        /// </summary>
        /// <param name="k">k</param>
        /// <param name="n">n</param>
        /// <returns>Binomial coefficient; nCk</returns>
        internal static long Binomial(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0) return 1;

            long r = 1;
            for (int d = 1; d <= k; ++d)
            {
                r *= n;
                r /= d;
                n -= 1;
            }

            return r;
        }

        /// <summary>
        /// Estimate the extra entropy in a token that comes from mixing upper and lowercase letters.
        /// This has been moved to a static function so that it can be used in multiple entropy calculations.
        /// </summary>
        /// <param name="word">The word to calculate uppercase entropy for</param>
        /// <returns>An estimation of the entropy gained from casing in <paramref name="word"/></returns>
        internal static double CalculateUppercaseEntropy(string word)
        {
            const string StartUpper = "^[A-Z][^A-Z]+$";
            const string EndUpper = "^[^A-Z]+[A-Z]$";
            const string AllUpper = "^[^a-z]+$";
            const string AllLower = "^[^A-Z]+$";

            if (Regex.IsMatch(word, AllLower)) return 0;

            // If the word is all uppercase add's only one bit of entropy, add only one bit for initial/end single cap only
            if (new[] { StartUpper, EndUpper, AllUpper }.Any(re => Regex.IsMatch(word, re))) return 1;

            var lowers = word.Where(c => 'a' <= c && c <= 'z').Count();
            var uppers = word.Where(c => 'A' <= c && c <= 'Z').Count();

            // Calculate numer of ways to capitalise (or inverse if there are fewer lowercase chars) and return lg for entropy
            return Math.Log(Enumerable.Range(0, Math.Min(uppers, lowers) + 1).Sum(i => PasswordScoring.Binomial(uppers + lowers, i)), 2);
        }
    }
}
