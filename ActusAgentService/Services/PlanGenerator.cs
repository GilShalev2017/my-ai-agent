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
    /// <summary>
    /// Gather relevant content (transcripts, alerts...)
    /// </summary>
    public class PlanGenerator : IPlanGenerator
    {
        private readonly IContentService _repo;

        public PlanGenerator(IContentService repo)
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
                CreatedAt = DateTime.UtcNow
            };

            // Handle multiple topics or fallback
            var topics = plan.Entities;
            var date = plan.Dates.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Collect transcript lines for each topic
            var allLines = new List<string>();
            foreach (var topic in topics.DefaultIfEmpty(""))
            {
                var lines = await _repo.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date);
                allLines.AddRange(lines);
            }

            plan.TranscriptLines = allLines.Distinct().ToList();

            // Fetch relevant alerts if any
            plan.Alerts = await _repo.GetAlertsByDateAsync(date);

            return plan;
        }

        private string ComputeQueryHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        /*Prev code that tool care of plan.Intents and created plan.FinalPrompt
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
                Alerts = new List<string>(),
                TranscriptLines = new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            // Determine topics and date
            var topics = plan.Entities.Any() ? plan.Entities : new List<string> { "" };
            var date = plan.Dates.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Collect transcripts for each topic
            var transcriptTasks = topics.Select(topic =>
                _repo.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date)
            );

            var allTranscripts = await Task.WhenAll(transcriptTasks);

            plan.TranscriptLines = allTranscripts.SelectMany(t => t).Distinct().ToList();

            // Optional: Include alert-related content if alert-related intent exists
            if (plan.Intents.Contains("alert") || plan.Intents.Any(i => i.Contains("alert", StringComparison.OrdinalIgnoreCase)))
            {
                // Let's assume your repo can provide alert text as well
                var alerts = await _repo.GetAlertsByDateAsync(date);
               
                plan.Alerts.AddRange(alerts);
            }

            // Build dynamic action based on intent
            string action = plan.Intents.FirstOrDefault()?.ToLower() ?? "analyze";
            action = action switch
            {
                var a when a.Contains("summary") || a.Contains("summarize") => "Summarize",
                var a when a.Contains("keyword") => "Find keywords",
                var a when a.Contains("emotion") => "Analyze emotions",
                var a when a.Contains("alert") => "Identify alert-worthy content",
                _ => "Analyze"
            };

            // Build prompt in natural language
            var promptBuilder = new StringBuilder();
        
            promptBuilder.AppendLine($"{action} the following transcripts for topics: {string.Join(", ", topics)} on {date}.");

            if (plan.Alerts.Any())
            {
                promptBuilder.AppendLine("\nBe aware of the following known alerts:");
                foreach (var alert in plan.Alerts)
                    promptBuilder.AppendLine($"- {alert}");
            }

            promptBuilder.AppendLine("\nTranscript content:");
            foreach (var line in plan.TranscriptLines.Take(100)) // optional truncation
                promptBuilder.AppendLine($"- {line}");

            plan.FinalPrompt = promptBuilder.ToString();

            return plan;
        }*/
      
    }
}