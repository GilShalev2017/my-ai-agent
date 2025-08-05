using ActusAgentService.Models;
using System.Text;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public interface IEntityExtractor
    {
        Task<QueryIntentContext> ExtractAsync(string userQuery);
    }
    public class EntityExtractor : IEntityExtractor
    {
        private readonly OpenAiService _openAiService;
        private readonly IDateNormalizer _dateNormalizer;

        public EntityExtractor(OpenAiService openAiService, IDateNormalizer dateNormalizer)
        {
            _openAiService = openAiService;
            _dateNormalizer = dateNormalizer;
        }

        public async Task<QueryIntentContext> ExtractAsync(string userQuery)
        {
            var systemPrompt = $@"
You are a smart assistant. Analyze the following user query and extract:

- intents (list of strings)
- entities (list of objects with 'entity' and 'type')
- dates (list of objects with 'date' and 'type')
- sources (list of objects with 'source' and 'type')

Respond only in **valid JSON**. Do not include any explanation or comments.

User query: ""{userQuery}""
";


            var jsonResponse = await _openAiService.GetChatCompletionAsync(systemPrompt);

            try
            {
                var context = JsonSerializer.Deserialize<QueryIntentContext>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (context == null)
                    throw new Exception("Deserialized context is null. Raw response: " + jsonResponse);

                context.OriginalQuery = userQuery;


                var dateStrings = context.Dates.Select(d => d.Date).ToList();

                context.Dates = _dateNormalizer.Normalize(dateStrings)
                                               .Select(date => new DateEntity { Date = date, Type = "Normalized" })
                                               .ToList();

                return context;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse model response as JSON. Raw content:\n" + jsonResponse, ex);
            }
        }
    }
}

