using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public class PlanGenerator
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
                Intents = context.Intents ?? new List<string>(),
                Entities = context.Entities ?? new List<string>(),
                Dates = context.Dates ?? new List<string>(),
                Sources = context.Sources ?? new List<string>(), // optional: depends on extraction
                Alerts = new List<string>(), // will populate later if needed
                TranscriptLines = new List<string>(), // will populate below
                CreatedAt = DateTime.UtcNow
            };

            // Get first relevant topic and date
            var topic = plan.Entities.FirstOrDefault() ?? "";
            var date = plan.Dates.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Get transcripts (or alerts if needed)
            var transcripts = await _repo.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date);
            plan.TranscriptLines = transcripts;

            // Compose final prompt (optional here or outside)
            plan.FinalPrompt = $"Summarize the following transcripts related to {topic} on {date}:\n" +
                               string.Join("\n", plan.TranscriptLines);

            return plan;
        }

        private string ComputeQueryHash(string query)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(query);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

    }
}