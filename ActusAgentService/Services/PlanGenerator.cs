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
            var filter = new JobResultFilter
            {
                Operation = "Transcription"
            };

            if (context.Dates != null && context.Dates.Count > 0)
            {
                DateTime? minStart = null;
                DateTime? maxEnd = null;

                foreach (var dateEntity in context.Dates)
                {
                    DateTime? startDateTime = null;
                    DateTime? endDateTime = null;

                    // Handle range first (StartDate + EndDate)
                    if (!string.IsNullOrWhiteSpace(dateEntity.StartDate) &&
                        DateTime.TryParse(dateEntity.StartDate, out var startDateBase))
                    {
                        startDateTime = startDateBase.Date;
                        if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) &&
                            TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
                        {
                            startDateTime = startDateTime.Value.Add(startTime);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(dateEntity.EndDate) &&
                        DateTime.TryParse(dateEntity.EndDate, out var endDateBase))
                    {
                        endDateTime = endDateBase.Date;
                        if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) &&
                            TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
                        {
                            endDateTime = endDateTime.Value.Add(endTime);
                        }
                        else
                        {
                            // If no time given, assume end of day
                            endDateTime = endDateTime.Value.AddDays(1).AddTicks(-1);
                        }
                    }

                    // Fallback to single Date (for backward compatibility)
                    if (startDateTime == null && !string.IsNullOrWhiteSpace(dateEntity.Date) &&
                        DateTime.TryParse(dateEntity.Date, out var baseDate))
                    {
                        startDateTime = baseDate.Date;
                        endDateTime = baseDate.Date.AddDays(1).AddTicks(-1); // default to whole day

                        if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) &&
                            TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
                        {
                            startDateTime = baseDate.Date + startTime;
                        }

                        if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) &&
                            TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
                        {
                            endDateTime = baseDate.Date + endTime;
                        }
                    }

                    // Skip if we still don't have valid range
                    if (startDateTime == null || endDateTime == null)
                        continue;

                    if (minStart == null || startDateTime < minStart)
                        minStart = startDateTime;

                    if (maxEnd == null || endDateTime > maxEnd)
                        maxEnd = endDateTime;
                }

                if (minStart.HasValue && maxEnd.HasValue)
                {
                    filter.Start = minStart.Value;
                    filter.End = maxEnd.Value;
                }
            }

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
                            if (context.IsTimeCodeNeeded)
                            {
                                var timeCodedTranscript = $"{line.StartTime} - {line.EndTime}\n{line.Text.Trim()}";
                                allLines.Add(timeCodedTranscript);
                            }
                            else
                            { 
                                allLines.Add(line.Text.Trim());
                            }
                        }
                    }
                }
            }

            var plan = new QueryPlan
            {
                QueryHash = ComputeQueryHash(context.OriginalQuery),
                UserQuery = context.OriginalQuery,
                Intents = context.Intents ?? new(),
                Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
                Dates = context.Dates?.Select(d => d.Date ?? d.StartDate).ToList() ?? new(),
                RawDates = context.Dates ?? new List<DateEntity>(),
                Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
                TranscriptLines = allLines.Distinct().ToList(),
                Filter = filter,
                CreatedAt = DateTime.UtcNow
            };

            return plan;
        }

        private string ComputeQueryHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
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
