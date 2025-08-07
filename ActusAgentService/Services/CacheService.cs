using ActusAgentService.Models;

namespace ActusAgentService.Services
{
    public class CacheService
    {
    }

    public interface IQueryPlanCache
    {
        Task<QueryPlan?> GetCachedPlanAsync(string userQuery);
        Task CachePlanAsync(string userQuery, QueryPlan plan);
    }

    public class InMemoryQueryPlanCache : IQueryPlanCache
    {
        private readonly Dictionary<string, QueryPlan> _cache = new();

        public Task<QueryPlan?> GetCachedPlanAsync(string userQuery)
        {
            _cache.TryGetValue(userQuery, out var plan);
            return Task.FromResult(plan);
        }

        public Task CachePlanAsync(string userQuery, QueryPlan plan)
        {
            _cache[userQuery] = plan;
            return Task.CompletedTask;
        }
    }
}
