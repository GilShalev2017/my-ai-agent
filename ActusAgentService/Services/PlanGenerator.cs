using ActusAgentService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public interface IPlanGenerator
    {
        Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context);
    }

    public class PlanGenerator : IPlanGenerator
    {
        private readonly TranscriptRepository _repo;

        public PlanGenerator(TranscriptRepository repo)
        {
            _repo = repo;
        }

        public async Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context)
        {
            var plan = new QueryPlan
            {
                QueryHash = ComputeQueryHash(context.OriginalQuery),
                UserQuery = context.OriginalQuery,
                Intents = context.Intents ?? new(),
                Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
                Dates = context.Dates?.Select(d => d.Date).ToList() ?? new(),
                Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
                Alerts = new(),
                TranscriptLines = new(),
                CreatedAt = DateTime.UtcNow
            };

            var topic = plan.Entities.FirstOrDefault() ?? "";
            var date = plan.Dates.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            plan.TranscriptLines = await _repo.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date);

            plan.FinalPrompt = $"Summarize transcripts for topic '{topic}' on {date}: {string.Join("\n", plan.TranscriptLines)} ";

            return plan;
        }

        private static string ComputeQueryHash(string query)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(query)));
        }
    }
}