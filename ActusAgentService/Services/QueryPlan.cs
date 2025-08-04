using System.Security.Cryptography;
using System.Text;

namespace ActusAgentService.Services
{
    public class QueryPlan
    {
        public string QueryHash { get; set; } // hash of user query to cache
        public string UserQuery { get; set; }

        public List<string> Intents { get; set; }
        public List<string> Entities { get; set; }
        public List<string> Dates { get; set; }
        public List<string> Sources { get; set; }

        public List<string> Alerts { get; set; }
        public List<string> TranscriptLines { get; set; }

        public string FinalPrompt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsExpired => (DateTime.UtcNow - CreatedAt).TotalMinutes > 10; // optional TTL
    }

    public class QueryPlanCache
    {
        private static readonly Dictionary<string, QueryPlan> _cache = new();

        public static string GetHash(string query)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(query);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        public static QueryPlan Get(string query)
        {
            var hash = GetHash(query);
            if (_cache.TryGetValue(hash, out var plan) && !plan.IsExpired)
                return plan;
            return null;
        }

        public static void Store(QueryPlan plan)
        {
            _cache[plan.QueryHash] = plan;
        }
    }

}
