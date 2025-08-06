using ActusAgentService.Models;
using ActusAgentService.Models.ActIntelligence;
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
        private readonly IContentService _contentService;

        public PlanGenerator(IContentService repo)
        {
            _contentService = repo;
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

            var date = plan.Dates.FirstOrDefault();

            // Build a JobResultFilter using channel ID and start/end time (if possible)
            var filter = new JobResultFilter
            {
                Operation = "Transcription"
            };

            // Set ChannelId if available
            //if (int.TryParse(plan.Sources.FirstOrDefault(), out var channelId))
            //{
            //    filter.ChannelIds.Add(channelId);
            //}

            // Set Start/End date range
            if (DateTime.TryParse(date, out var parsedDate))
            {
                // Assuming you want to include the whole day
                filter.Start = parsedDate.Date;
                filter.End = parsedDate.Date.AddDays(1).AddTicks(-1); // End of the day
            }

            // Fetch transcripts based on the narrowed filter
            var transcripts = await _contentService.GetFilteredTranscriptsAsync(filter);

            var allLines = new List<string>();

            foreach (var transcript in transcripts)
            {
                if (transcript.Content != null)
                {
                    foreach (var line in transcript.Content)
                    {
                        if (!string.IsNullOrWhiteSpace(line.Text))
                        {
                            allLines.Add(line.Text.Trim());
                        }
                    }
                }
            }

            plan.TranscriptLines = allLines.Distinct().ToList();

            // Fetch relevant alerts (same date logic)
            //if (DateTime.TryParse(date, out var alertDate))
            //{
            //    plan.Alerts = await _contentService.GetAlertsByDateAsync(alertDate.ToString("yyyy-MM-dd"));
            //}

            return plan;
        }

        //public async Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context)
        //{
        //    var plan = new QueryPlan
        //    {
        //        QueryHash = ComputeQueryHash(context.OriginalQuery),
        //        UserQuery = context.OriginalQuery,
        //        Intents = context.Intents ?? new(),
        //        Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
        //        Dates = context.Dates?.Select(d => d.Date).ToList() ?? new(),
        //        Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    // Handle multiple topics or fallback
        //    //var topics = plan.Entities;
        //    var date = plan.Dates.FirstOrDefault() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        //    // Collect transcript lines for each topic
        //    var allLines = new List<string>();

        //    JobResultFilter jrf = new JobResultFilter() { Operation = "Transcription" };

        //    var transcripts = await _contentService.GetFilteredTranscriptsAsync(jrf);

        //    //foreach (var topic in topics.DefaultIfEmpty(""))
        //    //{
        //    //    var lines = await _contentService.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date);

        //    //    JobResultFilter jrf = new JobResultFilter() { Operation = "Transcription" };

        //    //    var transcripts = await _contentService.GetFilteredTranscriptsAsync(jrf);

        //    //    allLines.AddRange(lines);
        //    //}

        //    plan.TranscriptLines = allLines.Distinct().ToList();

        //    // Fetch relevant alerts if any
        //    plan.Alerts = await _contentService.GetAlertsByDateAsync(date);

        //    return plan;
        //}

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
                _contentService.GetTranscriptsByTopicAndDateAsync(context.OriginalQuery, topic, date)
            );

            var allTranscripts = await Task.WhenAll(transcriptTasks);

            plan.TranscriptLines = allTranscripts.SelectMany(t => t).Distinct().ToList();

            // Optional: Include alert-related content if alert-related intent exists
            if (plan.Intents.Contains("alert") || plan.Intents.Any(i => i.Contains("alert", StringComparison.OrdinalIgnoreCase)))
            {
                // Let's assume your repo can provide alert text as well
                var alerts = await _contentService.GetAlertsByDateAsync(date);
               
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