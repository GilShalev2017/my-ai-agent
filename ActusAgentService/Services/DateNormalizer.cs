using ActusAgentService.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ActusAgentService.Services
{
    public interface IDateNormalizer
    {
        List<string> Normalize(List<string> datePhrases);
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek endOfWeek)
        {
            return dt.StartOfWeek(endOfWeek).AddDays(6);
        }
    }
    public class DateNormalizer : IDateNormalizer
    {
        public List<string> Normalize(List<string> datePhrases)
        {
            var result = new List<string>();

            foreach (var phrase in datePhrases)
            {
                var normalized = NormalizePhrase(phrase);
                if (!string.IsNullOrEmpty(normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private string NormalizePhrase(string phrase)
        {
            phrase = phrase.ToLowerInvariant().Trim();
            var today = DateTime.UtcNow.Date;

            // Predefined phrases
            var normalized = phrase switch
            {
                "today" => today.ToString("yyyy-MM-dd"),
                "yesterday" => today.AddDays(-1).ToString("yyyy-MM-dd"),
                "tomorrow" => today.AddDays(1).ToString("yyyy-MM-dd"),
                "last week" => $"{today.AddDays(-7):yyyy-MM-dd} to {today:yyyy-MM-dd}",
                "this week" => $"{today.StartOfWeek(DayOfWeek.Monday):yyyy-MM-dd} to {today.EndOfWeek(DayOfWeek.Sunday):yyyy-MM-dd}",
                "next week" => $"{today.AddDays(7).StartOfWeek(DayOfWeek.Monday):yyyy-MM-dd} to {today.AddDays(7).EndOfWeek(DayOfWeek.Sunday):yyyy-MM-dd}",
                _ => TryParseDate(phrase)
            };

            return normalized;
        }

        private string TryParseDate(string phrase)
        {
            // Clean standard date formats
            var cleaned = Regex.Replace(phrase, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
            Console.WriteLine($"Trying to parse cleaned date phrase: '{cleaned}'");

            if (DateTime.TryParse(cleaned, out var date))
                return date.ToString("yyyy-MM-dd");

            // Try parsing natural language like "the fifth of July"
            date = TryParseNaturalLanguageDate(phrase);
            if (date != default)
                return date.ToString("yyyy-MM-dd");

            return string.Empty;
        }

        private DateTime TryParseNaturalLanguageDate(string phrase)
        {
            // Match patterns like "the fifth of July" or "fifth of July"
            var match = Regex.Match(phrase, @"(?:the\s+)?(?<dayWord>\w+)(?:\s+of\s+)(?<month>\w+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return default;

            var dayWord = match.Groups["dayWord"].Value;
            var month = match.Groups["month"].Value;

            if (!TryConvertDayWordToNumber(dayWord, out int day))
                return default;

            if (!DateTime.TryParseExact(month, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthDate))
                return default;

            var year = DateTime.UtcNow.Year;
            try
            {
                return new DateTime(year, monthDate.Month, day);
            }
            catch
            {
                return default;
            }
        }

        private bool TryConvertDayWordToNumber(string word, out int number)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["first"] = 1,
                ["second"] = 2,
                ["third"] = 3,
                ["fourth"] = 4,
                ["fifth"] = 5,
                ["sixth"] = 6,
                ["seventh"] = 7,
                ["eighth"] = 8,
                ["ninth"] = 9,
                ["tenth"] = 10,
                ["eleventh"] = 11,
                ["twelfth"] = 12,
                ["thirteenth"] = 13,
                ["fourteenth"] = 14,
                ["fifteenth"] = 15,
                ["sixteenth"] = 16,
                ["seventeenth"] = 17,
                ["eighteenth"] = 18,
                ["nineteenth"] = 19,
                ["twentieth"] = 20,
                ["twenty-first"] = 21,
                ["twenty second"] = 22,
                ["twenty-third"] = 23,
                ["twenty-fourth"] = 24,
                ["twenty-fifth"] = 25,
                ["twenty-sixth"] = 26,
                ["twenty-seventh"] = 27,
                ["twenty-eighth"] = 28,
                ["twenty-ninth"] = 29,
                ["thirtieth"] = 30,
                ["thirty-first"] = 31
            };

            return map.TryGetValue(word.Replace("-", " "), out number);
        }
    }

}
