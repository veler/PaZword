using PaZword.Core.Security.PasswordStrengthEvaluator.Matcher;
using System.Collections.Generic;

namespace PaZword.Core.Security.PasswordStrengthEvaluator
{
    /// <summary>
    /// <para>This matcher factory will use all of the default password matchers.</para>
    /// 
    /// <para>Default dictionary matchers use the built-in word lists: passwords, english, male_names, female_names, surnames</para>
    /// <para>Also matching against: user data, all dictionaries with l33t substitutions</para>
    /// <para>Other default matchers: repeats, sequences, digits, years, dates, spatial</para>
    /// 
    /// <para>See <see cref="IMatcher"/> and the classes that implement it for more information on each kind of pattern matcher.</para>
    /// </summary>
    internal sealed class DefaultMatcherFactory
    {
        private readonly List<IMatcher> matchers;

        /// <summary>
        /// Create a matcher factory that uses the default list of pattern matchers
        /// </summary>
        internal DefaultMatcherFactory()
        {
            var dictionaryMatchers = new List<DictionaryMatcher>() {
                new DictionaryMatcher("passwords", "passwords.lst"),
                new DictionaryMatcher("english", "english.lst"),
                new DictionaryMatcher("male_names", "male_names.lst"),
                new DictionaryMatcher("female_names", "female_names.lst"),
                new DictionaryMatcher("surnames", "surnames.lst"),
            };

            matchers = new List<IMatcher> {
                new RepeatMatcher(),
                new SequenceMatcher(),
                new RegexMatcher("\\d{3,}", 10, true, "digits"),
                new RegexMatcher("19\\d\\d|200\\d|201\\d", 119, false, "year"),
                new DateMatcher(),
                new SpatialMatcher()
            };

            matchers.AddRange(dictionaryMatchers);
            matchers.Add(new L33tMatcher(dictionaryMatchers));
        }

        /// <summary>
        /// Get instances of pattern matchers.
        /// </summary>
        /// <returns>Enumerable of matchers to use</returns>
        internal IEnumerable<IMatcher> GetMatchers()
        {
            return matchers;
        }
    }
}
