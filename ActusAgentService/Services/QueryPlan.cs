using ActusAgentService.Models;
using System.Security.Cryptography;
using System.Text;

namespace ActusAgentService.Services
{
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
