using ActusAgentService.Models;
using ActusAgentService.Models.ActIntelligence;
using DotNext;
using System.Security.Cryptography;
using System.Text;


namespace ActusAgentService.Services
{
    public interface IPlanGenerator
    {
        Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context);
    }

    public class PlanGenerator : IPlanGenerator
    {
        private readonly IContentService _contentService;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IVectorDBRepository _vectorDbRepository;

        public PlanGenerator(IContentService contentService, IEmbeddingProvider embeddingProvider, IVectorDBRepository vectorDbRepository)
        {
            _contentService = contentService;
            _embeddingProvider = embeddingProvider;
            _vectorDbRepository = vectorDbRepository;
        }

        private JobResultFilter GetFilterByDateAndChannels(QueryIntentContext context)
        {
            var filter = new JobResultFilter
            {
                Operation = "Transcription"
            };

            if (context.Dates != null && context.Dates.Any())
            {
                DateTime? minStart = null;
                DateTime? maxEnd = null;

                foreach (var dateEntity in context.Dates)
                {
                    DateTime? startDateTime = null;
                    DateTime? endDateTime = null;

                    // Handle date ranges (StartDate + EndDate)
                    if (!string.IsNullOrWhiteSpace(dateEntity.StartDate) && DateTime.TryParse(dateEntity.StartDate, out var startDateBase))
                    {
                        startDateTime = startDateBase.Date;
                        if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) && TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
                        {
                            startDateTime = startDateTime.Value.Add(startTime);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(dateEntity.EndDate) && DateTime.TryParse(dateEntity.EndDate, out var endDateBase))
                    {
                        endDateTime = endDateBase.Date;
                        if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) && TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
                        {
                            endDateTime = endDateTime.Value.Add(endTime);
                        }
                        else
                        {
                            endDateTime = endDateTime.Value.AddDays(1).AddTicks(-1);
                        }
                    }

                    // Fallback to a single Date
                    if (startDateTime == null && !string.IsNullOrWhiteSpace(dateEntity.Date) && DateTime.TryParse(dateEntity.Date, out var baseDate))
                    {
                        startDateTime = baseDate.Date;
                        endDateTime = baseDate.Date.AddDays(1).AddTicks(-1);

                        if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) && TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
                        {
                            startDateTime = baseDate.Date + startTime;
                        }
                        if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) && TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
                        {
                            endDateTime = baseDate.Date + endTime;
                        }
                    }

                    if (startDateTime.HasValue && endDateTime.HasValue)
                    {
                        if (minStart == null || startDateTime < minStart)
                            minStart = startDateTime;
                        if (maxEnd == null || endDateTime > maxEnd)
                            maxEnd = endDateTime;
                    }
                }

                if (minStart.HasValue && maxEnd.HasValue)
                {
                    filter.Start = minStart.Value;
                    filter.End = maxEnd.Value;
                }
            }

            // Add ChannelIds if they exist
            //if (context.Sources != null && context.Sources.Any())
            //{
            //    filter.ChannelIds = context.Sources
            //        .Where(s => int.TryParse(s.Source, out _))
            //        .Select(s => int.Parse(s.Source))
            //        .ToList();
            //}

            return filter;
        }

        private List<string> PullJobTranscripts(List<JobResult> jobTranscripts, bool isTimeCodeNeeded)
        {
            var allLines = new List<string>();

            foreach (var transcript in jobTranscripts)
            {
                if (transcript.Content != null)
                {
                    foreach (var line in transcript.Content)
                    {
                        if (!string.IsNullOrWhiteSpace(line.Text))
                        {
                            if (isTimeCodeNeeded)
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
            return allLines;
        }

        public async Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context)
        {
            var filter = GetFilterByDateAndChannels(context);

            List<JobResult> relevantTranscripts;

            // This is the core logic. You decide based on a semantic query or keywords
            if (!string.IsNullOrWhiteSpace(context.OriginalQuery))
            {
                // Use vector search if a semantic query is present
                relevantTranscripts = await _contentService.GetTranscriptsBySemanticSearchAndDateAsync(context.OriginalQuery, filter);
            }
            else
            {
                // Fallback to simple keyword/date filtering
                // Here you might need a different method on IContentService
                // I've added one below for clarity
                relevantTranscripts = await _contentService.GetFilteredTranscriptsAsync(filter);
            }

            if (!relevantTranscripts.Any())
            {
                return new QueryPlan
                {
                    QueryHash = ComputeQueryHash(context.OriginalQuery),
                    UserQuery = context.OriginalQuery,
                    TranscriptLines = new List<string>(),
                    Filter = filter,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Step 4: flatten transcript text for output
            var transcripts = PullJobTranscripts(
                relevantTranscripts,
                context.IsTimeCodeNeeded
            );

            return new QueryPlan
            {
                QueryHash = ComputeQueryHash(context.OriginalQuery),
                UserQuery = context.OriginalQuery,
                Intents = context.Intents ?? new(),
                Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
                Dates = context.Dates?.Select(d => d.Date ?? d.StartDate).ToList() ?? new(),
                RawDates = context.Dates ?? new List<DateEntity>(),
                Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
                TranscriptLines = transcripts.Distinct().ToList(),
                Filter = filter,
                CreatedAt = DateTime.UtcNow
            };
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
    //public interface IPlanGenerator
    //{
    //    Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context);
    //}
    ///// <summary>
    ///// Gather relevant content (transcripts, alerts...)
    ///// </summary>
    //public class PlanGenerator : IPlanGenerator
    //{
    //    private readonly IContentService _contentService;
    //    private readonly IEmbeddingProvider _embeddingProvider;
    //    private readonly IVectorDBRepository _vectorDbRepository;

    //    public PlanGenerator(IContentService contentService, IEmbeddingProvider embeddingProvider, IVectorDBRepository vectorDbRepository   )
    //    {
    //        _contentService = contentService;
    //        _embeddingProvider = embeddingProvider;
    //        _vectorDbRepository = vectorDbRepository;
    //    }

    //    private JobResultFilter GetFilterByDateAndChannels(QueryIntentContext context)
    //    {
    //        var filter = new JobResultFilter
    //        {
    //            Operation = "Transcription"
    //        };

    //        if (context.Dates != null && context.Dates.Count > 0)
    //        {
    //            DateTime? minStart = null;
    //            DateTime? maxEnd = null;

    //            foreach (var dateEntity in context.Dates)
    //            {
    //                DateTime? startDateTime = null;
    //                DateTime? endDateTime = null;

    //                // Handle range first (StartDate + EndDate)
    //                if (!string.IsNullOrWhiteSpace(dateEntity.StartDate) &&
    //                    DateTime.TryParse(dateEntity.StartDate, out var startDateBase))
    //                {
    //                    startDateTime = startDateBase.Date;
    //                    if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) &&
    //                        TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
    //                    {
    //                        startDateTime = startDateTime.Value.Add(startTime);
    //                    }
    //                }

    //                if (!string.IsNullOrWhiteSpace(dateEntity.EndDate) &&
    //                    DateTime.TryParse(dateEntity.EndDate, out var endDateBase))
    //                {
    //                    endDateTime = endDateBase.Date;
    //                    if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) &&
    //                        TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
    //                    {
    //                        endDateTime = endDateTime.Value.Add(endTime);
    //                    }
    //                    else
    //                    {
    //                        // If no time given, assume end of day
    //                        endDateTime = endDateTime.Value.AddDays(1).AddTicks(-1);
    //                    }
    //                }

    //                // Fallback to single Date (for backward compatibility)
    //                if (startDateTime == null && !string.IsNullOrWhiteSpace(dateEntity.Date) &&
    //                    DateTime.TryParse(dateEntity.Date, out var baseDate))
    //                {
    //                    startDateTime = baseDate.Date;
    //                    endDateTime = baseDate.Date.AddDays(1).AddTicks(-1); // default to whole day

    //                    if (!string.IsNullOrWhiteSpace(dateEntity.StartTime) &&
    //                        TimeSpan.TryParse(dateEntity.StartTime, out var startTime))
    //                    {
    //                        startDateTime = baseDate.Date + startTime;
    //                    }

    //                    if (!string.IsNullOrWhiteSpace(dateEntity.EndTime) &&
    //                        TimeSpan.TryParse(dateEntity.EndTime, out var endTime))
    //                    {
    //                        endDateTime = baseDate.Date + endTime;
    //                    }
    //                }

    //                // Skip if we still don't have valid range
    //                if (startDateTime == null || endDateTime == null)
    //                    continue;

    //                if (minStart == null || startDateTime < minStart)
    //                    minStart = startDateTime;

    //                if (maxEnd == null || endDateTime > maxEnd)
    //                    maxEnd = endDateTime;
    //            }

    //            if (minStart.HasValue && maxEnd.HasValue)
    //            {
    //                filter.Start = minStart.Value;
    //                filter.End = maxEnd.Value;
    //            }
    //        }

    //        return filter;
    //    }

    //    private  List<string> PullJobTranscripts(List<JobResult> jobTranscripts, bool isTimeCodeNeeded)
    //    {
    //        var allLines = new List<string>();

    //        foreach (var transcript in jobTranscripts)
    //        {
    //            if (transcript.Content != null)
    //            {
    //                foreach (var line in transcript.Content)
    //                {
    //                    if (!string.IsNullOrWhiteSpace(line.Text))
    //                    {
    //                        if (isTimeCodeNeeded)
    //                        {
    //                            var timeCodedTranscript = $"{line.StartTime} - {line.EndTime}\n{line.Text.Trim()}";
    //                            allLines.Add(timeCodedTranscript);
    //                        }
    //                        else
    //                        {
    //                            allLines.Add(line.Text.Trim());
    //                        }
    //                    }
    //                }
    //            }
    //        }

    //        return allLines;
    //    }

    //    public async Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context)
    //    {
    //        var filter = GetFilterByDateAndChannels(context);

    //        // Step 1: filter by date/channel
    //        var jobTranscripts = await _contentService.GetFilteredTranscriptsAsync(filter);
    //        if (!jobTranscripts.Any())
    //        {
    //            return new QueryPlan
    //            {
    //                QueryHash = ComputeQueryHash(context.OriginalQuery),
    //                UserQuery = context.OriginalQuery,
    //                TranscriptLines = new List<string>(),
    //                Filter = filter,
    //                CreatedAt = DateTime.UtcNow
    //            };
    //        }

    //        // Step 2: embed user query
    //        var queryEmbedding = await _embeddingProvider.EmbedAsync(context.OriginalQuery);

    //        // Step 3: run semantic search on the filtered transcripts
    //        var transcriptEmbeddings = jobTranscripts
    //            .Where(t => t.Embedding != null && t.Embedding.Length > 0)
    //            .Select(t => new VectorItem
    //            {
    //                Id = t.Id ?? Guid.NewGuid().ToString(),
    //                Embedding = t.Embedding!,
    //                Payload = t
    //            })
    //            .ToList();

    //        int topN = 10;
    //        var topMatches = _vectorDbRepository.SearchSimilarAsync(queryEmbedding, transcriptEmbeddings, topN);

    //        // Step 4: flatten transcript text for output
    //        var transcripts = PullJobTranscripts(
    //            topMatches.Select(m => (JobResult)m.Payload).ToList(),
    //            context.IsTimeCodeNeeded
    //        );

    //        return new QueryPlan
    //        {
    //            QueryHash = ComputeQueryHash(context.OriginalQuery),
    //            UserQuery = context.OriginalQuery,
    //            Intents = context.Intents ?? new(),
    //            Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
    //            Dates = context.Dates?.Select(d => d.Date ?? d.StartDate).ToList() ?? new(),
    //            RawDates = context.Dates ?? new List<DateEntity>(),
    //            Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
    //            TranscriptLines = transcripts.Distinct().ToList(),
    //            Filter = filter,
    //            CreatedAt = DateTime.UtcNow
    //        };
    //    }

    //    public async Task<List<JobResult>> GetRelevantTranscriptsChronologicallyAsync(string userQuery, JobResultFilter filter, int topN = 20)
    //    {
    //        // Step 1: Filter transcripts
    //        var filteredTranscripts = await GetFilteredTranscriptsAsync(filter);
    //        if (!filteredTranscripts.Any())
    //            return filteredTranscripts;

    //        // Step 2: Embed the user query
    //        var queryEmbedding = await _embeddingProvider.EmbedAsync(userQuery);

    //        // Step 3: Prepare vector items from filtered transcripts
    //        var transcriptEmbeddings = filteredTranscripts
    //            .Where(t => t.Embedding != null && t.Embedding.Length > 0)
    //            .Select(t => new VectorItem
    //            {
    //                Id = t.Id.ToString(),
    //                Embedding = t.Embedding,
    //                Payload = t
    //            })
    //            .ToList();

    //        // Step 4: Perform similarity search
    //        var topMatches = _vectorDbRepository.Search(queryEmbedding, transcriptEmbeddings, topN);

    //        // Step 5: Get the JobResult objects from matches
    //        var matchedResults = topMatches.Select(m => (JobResult)m.Payload).ToList();

    //        // Step 6: Sort them by Start time for chronological order
    //        return matchedResults.OrderBy(r => r.Start).ToList();
    //    }

    //    private string ComputeQueryHash(string input)
    //    {
    //        using var sha256 = SHA256.Create();
    //        var bytes = Encoding.UTF8.GetBytes(input);
    //        var hash = sha256.ComputeHash(bytes);
    //        return Convert.ToBase64String(hash);
    //    }
    //}


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


//public async Task<QueryPlan> GeneratePlanAsync(QueryIntentContext context)
//{
//    var filter = GetFilterByDateAndChannels(context);

//    var jobTranscripts = await _contentService.GetFilteredTranscriptsAsync(filter);

//    var transcripts = PullJobTranscripts(jobTranscripts, context.IsTimeCodeNeeded);

//    var plan = new QueryPlan
//    {
//        QueryHash = ComputeQueryHash(context.OriginalQuery),
//        UserQuery = context.OriginalQuery,
//        Intents = context.Intents ?? new(),
//        Entities = context.Entities?.Select(e => e.EntityName).ToList() ?? new(),
//        Dates = context.Dates?.Select(d => d.Date ?? d.StartDate).ToList() ?? new(),
//        RawDates = context.Dates ?? new List<DateEntity>(),
//        Sources = context.Sources?.Select(s => s.Source).ToList() ?? new(),
//        TranscriptLines = transcripts.Distinct().ToList(),
//        Filter = filter,
//        CreatedAt = DateTime.UtcNow
//    };

//    return plan;
//}
