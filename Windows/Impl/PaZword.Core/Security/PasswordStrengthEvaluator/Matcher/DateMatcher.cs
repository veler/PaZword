using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PaZword.Core.Security.PasswordStrengthEvaluator.Matcher
{
    /// <summary>
    /// <para>This matcher attempts to guess dates, with and without date separators. e.g. 1197 (could be 1/1/97) through to 18/12/2015.</para>
    /// 
    /// <para>The format for matching dates is quite particular, and only detected years in the range 00-99 and 1900-2019 are considered by
    /// this matcher.</para>
    /// </summary>
    internal sealed class DateMatcher : IMatcher
    {
        // TODO: This whole matcher is a rather messy but works (just), could do with a touching up. In particular it does not provide matched date details for dates without separators


        const string DatePattern = "date";


        // The two regexes for matching dates with slashes is lifted directly from zxcvbn (matching.coffee about :400)
        const string DateWithSlashesSuffixPattern = @"  ( \d{1,2} )                         # day or month
  ( \s | - | / | \\ | _ | \. )        # separator
  ( \d{1,2} )                         # month or day
  \2                                  # same separator
  ( 19\d{2} | 200\d | 201\d | \d{2} ) # year";

        const string DateWithSlashesPrefixPattern = @"  ( 19\d{2} | 200\d | 201\d | \d{2} ) # year
  ( \s | - | / | \\ | _ | \. )        # separator
  ( \d{1,2} )                         # day or month
  \2                                  # same separator
  ( \d{1,2} )                         # month or day";

        /// <summary>
        /// Find date matches in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The passsord to check</param>
        /// <returns>An enumerable of date matches</returns>
        /// <seealso cref="DateMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            var matches = new List<Match>();

            var possibleDates = Regex.Matches(password, "\\d{4,8}"); // Slashless dates
            foreach (System.Text.RegularExpressions.Match dateMatch in possibleDates)
            {
                if (IsDate(dateMatch.Value)) matches.Add(new Match()
                {
                    Pattern = DatePattern,
                    i = dateMatch.Index,
                    j = dateMatch.Index + dateMatch.Length - 1,
                    Token = dateMatch.Value,
                    Entropy = CalculateEntropy(dateMatch.Value, null, false)
                });
            }

            var slashDatesSuffix = Regex.Matches(password, DateWithSlashesSuffixPattern, RegexOptions.IgnorePatternWhitespace);
            foreach (System.Text.RegularExpressions.Match dateMatch in slashDatesSuffix)
            {
                var year = dateMatch.Groups[4].Value.ToInt();
                var month = dateMatch.Groups[3].Value.ToInt(); // or day
                var day = dateMatch.Groups[1].Value.ToInt(); // or month

                // Do a quick check for month/day swap (e.g. US dates)
                if (12 <= month && month <= 31 && day <= 12) { var t = month; month = day; day = t; }

                if (IsDateInRange(year, month, day)) matches.Add(new DateMatch()
                {
                    Pattern = DatePattern,
                    i = dateMatch.Index,
                    j = dateMatch.Index + dateMatch.Length - 1,
                    Token = dateMatch.Value,
                    Entropy = CalculateEntropy(dateMatch.Value, year, true),
                    Separator = dateMatch.Groups[2].Value,
                    Year = year,
                    Month = month,
                    Day = day
                });
            }

            var slashDatesPrefix = Regex.Matches(password, DateWithSlashesPrefixPattern, RegexOptions.IgnorePatternWhitespace);
            foreach (System.Text.RegularExpressions.Match dateMatch in slashDatesPrefix)
            {
                var year = dateMatch.Groups[1].Value.ToInt();
                var month = dateMatch.Groups[3].Value.ToInt(); // or day
                var day = dateMatch.Groups[4].Value.ToInt(); // or month

                // Do a quick check for month/day swap (e.g. US dates)
                if (12 <= month && month <= 31 && day <= 12) { var t = month; month = day; day = t; }

                if (IsDateInRange(year, month, day)) matches.Add(new DateMatch()
                {
                    Pattern = DatePattern,
                    i = dateMatch.Index,
                    j = dateMatch.Index + dateMatch.Length - 1,
                    Token = dateMatch.Value,
                    Entropy = CalculateEntropy(dateMatch.Value, year, true),
                    Separator = dateMatch.Groups[2].Value,
                    Year = year,
                    Month = month,
                    Day = day
                });
            }

            return matches;
        }

        private static double CalculateEntropy(string match, int? year, bool separator)
        {
            // The entropy calculation is pretty straightforward

            // This is a slight departure from the zxcvbn case where the match has the actual year so the two-year vs four-year
            //   can always be known rather than guessed for strings without separators. 
            if (!year.HasValue)
            {
                // Guess year length from string length
                year = match.Length <= 6 ? 99 : 9999;
            }

            double entropy;
            if (year < 100) entropy = Math.Log(31 * 12 * 100, 2); // 100 years (two-digits)
            else entropy = Math.Log(31 * 12 * 119, 2); // 119 years (four digit years valid range)

            if (separator) entropy += 2; // Extra two bits for separator (/\...)

            return entropy;
        }

        /// <summary>
        /// Determine whether a string resembles a date (year first or year last)
        /// </summary>
        private static bool IsDate(string match)
        {
            bool isValid = false;

            // Try year length depending on match length. Length six should try both two and four digits

            if (match.Length <= 6)
            {
                // Try a two digit year, suffix and prefix
                isValid |= IsDateWithYearType(match, true, 2);
                isValid |= IsDateWithYearType(match, false, 2);
            }
            if (match.Length >= 6)
            {
                // Try a four digit year, suffix and prefix
                isValid |= IsDateWithYearType(match, true, 4);
                isValid |= IsDateWithYearType(match, false, 4);
            }

            return isValid;
        }

        private static bool IsDateWithYearType(string match, bool suffix, int yearLen)
        {
            int year;
            if (suffix) match.IntParseSubstring(match.Length - yearLen, yearLen, out year);
            else match.IntParseSubstring(0, yearLen, out year);

            if (suffix) return IsYearInRange(year) && IsDayMonthString(match.Substring(0, match.Length - yearLen));
            else return IsYearInRange(year) && IsDayMonthString(match.Substring(yearLen, match.Length - yearLen));
        }

        /// <summary>
        /// Determines whether a substring of a date string resembles a day and month (day-month or month-day)
        /// </summary>
        private static bool IsDayMonthString(string match)
        {
            int p1 = 0, p2 = 0;

            // Parse the day/month string into two parts
            if (match.Length == 2)
            {
                // e.g. 1 2 [1234]
                match.IntParseSubstring(0, 1, out p1);
                match.IntParseSubstring(1, 1, out p2);
            }
            else if (match.Length == 3)
            {
                // e.g. 1 12 [1234] or 12 1 [1234]

                match.IntParseSubstring(0, 1, out p1);
                match.IntParseSubstring(1, 2, out p2);

                // This one is a little different in that there's two ways to parse it so go one way first
                if (IsMonthDayInRange(p1, p2) || IsMonthDayInRange(p2, p1)) return true;

                match.IntParseSubstring(0, 2, out p1);
                match.IntParseSubstring(2, 1, out p2);
            }
            else if (match.Length == 4)
            {
                // e.g. 14 11 [1234]

                match.IntParseSubstring(0, 2, out p1);
                match.IntParseSubstring(2, 2, out p2);
            }

            // Check them both ways around to see if a valid day/month pair
            return IsMonthDayInRange(p1, p2) || IsMonthDayInRange(p2, p1);
        }

        private static bool IsDateInRange(int year, int month, int day)
        {
            return IsYearInRange(year) && IsMonthDayInRange(month, day);
        }

        // Two-digit years are allowed, otherwise in 1900-2019
        private static bool IsYearInRange(int year)
        {
            return (1900 <= year && year <= 2019) || (0 < year && year <= 99);
        }

        // Assume all months have 31 days, we only care that things look like dates not that they're completely valid
        private static bool IsMonthDayInRange(int month, int day)
        {
            return 1 <= month && month <= 12 && 1 <= day && day <= 31;
        }
    }
}
