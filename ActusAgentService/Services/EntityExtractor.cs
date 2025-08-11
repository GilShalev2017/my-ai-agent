using ActusAgentService.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ActusAgentService.Services
{
    public interface IEntityExtractor
    {
        Task<QueryIntentContext> ExtractAsync(string userQuery);
    }
    /// <summary>
    /// Extract QueryIntentContext from user query
    /// </summary>
    public class EntityExtractor : IEntityExtractor
    {
        private readonly IOpenAiService _openAiService;
        private readonly IDateNormalizer _dateNormalizer;

        public EntityExtractor(IOpenAiService openAiService, IDateNormalizer dateNormalizer)
        {
            _openAiService = openAiService;
            _dateNormalizer = dateNormalizer;
        }

        public async Task<QueryIntentContext> ExtractAsync(string userQuery)
        {
            var systemPrompt = @$"
You are a smart assistant. Analyze the following user query and extract:

- intents: list of strings.
- entities: list of objects with 'entity' and 'type'.
- dates: list of objects with this exact schema:
  - If the date is a single point in time:
    {{
      ""date"": ""yyyy-MM-dd"",
      ""type"": ""single"",
      ""startTime"": ""HH:mm:ss"" (optional),
      ""endTime"": ""HH:mm:ss"" (optional)
    }}
  - If the date is a range:
    {{
      ""type"": ""date_range"",
      ""startDate"": ""yyyy-MM-dd"",
      ""endDate"": ""yyyy-MM-dd"",
      ""startTime"": ""HH:mm:ss"" (optional),
      ""endTime"": ""HH:mm:ss"" (optional)
    }}

- sources: list of objects with 'source' and 'type'.

Rules:
- Use current year ({DateTime.UtcNow.Year}) if the year is not provided.
- Always follow the above structure exactly.
- Always include startDate and endDate if type is ""date_range"".

Return only valid JSON. No comments, no extra text.

User query: ""{userQuery}""
";

            var jsonResponse = await _openAiService.GetChatCompletionAsync("You are a helpful media assistant.",systemPrompt);

            try
            {
                var context = JsonSerializer.Deserialize<QueryIntentContext>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (context == null)
                    throw new Exception("Deserialized context is null. Raw response: " + jsonResponse);

                context.OriginalQuery = userQuery;

                context.RawJsonResponse = jsonResponse;


                //var dateStrings = context.Dates.Select(d => d.Date).ToList();

                //context.Dates = _dateNormalizer.Normalize(dateStrings)
                //                               .Select(date => new DateEntity { Date = date, Type = "Normalized" })
                //                               .ToList();

                return context;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse model response as JSON. Raw content:\n" + jsonResponse, ex);
            }
        }
    }
}

