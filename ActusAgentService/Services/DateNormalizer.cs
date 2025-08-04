using System.Text.RegularExpressions;

namespace ActusAgentService.Services
{
    public class DateNormalizer
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

            return phrase switch
            {
                "today" => today.ToString("yyyy-MM-dd"),
                "yesterday" => today.AddDays(-1).ToString("yyyy-MM-dd"),
                "tomorrow" => today.AddDays(1).ToString("yyyy-MM-dd"),
                "last week" => $"{today.AddDays(-7):yyyy-MM-dd} to {today:yyyy-MM-dd}",
                "this week" => $"{today.StartOfWeek(DayOfWeek.Monday):yyyy-MM-dd} to {today.EndOfWeek(DayOfWeek.Sunday):yyyy-MM-dd}",
                "next week" => $"{today.AddDays(7).StartOfWeek(DayOfWeek.Monday):yyyy-MM-dd} to {today.AddDays(7).EndOfWeek(DayOfWeek.Sunday):yyyy-MM-dd}",
                _ => TryParseDate(phrase)
            };
        }

        private string TryParseDate(string phrase)
        {
            var cleaned = Regex.Replace(phrase, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
            Console.WriteLine($"Trying to parse cleaned date phrase: '{cleaned}'");

            if (DateTime.TryParse(cleaned, out var date))
                return date.ToString("yyyy-MM-dd");

            return string.Empty;
        }

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

}
