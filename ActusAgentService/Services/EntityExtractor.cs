using ActusAgentService.Models;
using System.Text;
using System.Text.Json;

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
            //            var systemPrompt = $@"
            //You are a smart assistant. Analyze the following user query and extract:

            //- intents (list of strings)
            //- entities (list of objects with 'entity' and 'type')
            //- dates (list of objects with 'date', 'type', and optional 'startTime' and 'endTime' in 24-hour format)
            //- sources (list of objects with 'source' and 'type')

            //Return dates in **yyyy-MM-dd** format, and times in **HH:mm:ss** (24-hour clock).
            //For date ranges, include 'startDate' and 'endDate'.
            //For time ranges, include 'startTime' and 'endTime'.

            //Respond only in **valid JSON**. Do not include any explanation or comments.

            //User query: ""{userQuery}""
            //";
            var systemPrompt = $@"
You are a smart assistant. Analyze the following user query and extract:

- intents (list of strings)
- entities (list of objects with 'entity' and 'type')
- dates (list of objects with 'date', 'type', and optional 'startTime' and 'endTime' in 24-hour format)
- sources (list of objects with 'source' and 'type')

Return dates in **yyyy-MM-dd** format, and times in **HH:mm:ss** (24-hour clock).
If the user did not specify a year, assume the current year is {DateTime.UtcNow.Year}.
For date ranges, include 'startDate' and 'endDate'.
For time ranges, include 'startTime' and 'endTime'.

Respond only in **valid JSON**. Do not include any explanation or comments.

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

