using System.Text;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public class QueryIntentContext
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public List<string> Intents { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public List<string> Dates { get; set; } = new();  // Could be normalized later
        public List<string> Sources { get; set; } = new(); // e.g., "alerts", "transcripts"
    }

    public class EntityExtractor
    {
        private readonly OpenAiService _openAiService; // Inject your GPT/LLM service

        public EntityExtractor(OpenAiService openAiService)
        {
            _openAiService = openAiService;
        }

        public async Task<QueryIntentContext> ExtractAsync(string userQuery)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a smart assistant. Analyze the user query and extract:");
            sb.AppendLine("- intents: high-level tasks (e.g., summarization, alert_filter, emotion_analysis, keyword_search)");
            sb.AppendLine("- entities: key people, topics, or keywords mentioned");
            sb.AppendLine("- dates: specific days or ranges relevant to the query");
            sb.AppendLine("- sources: which data sources to use (alerts, transcripts, logs, email, etc.)");
            sb.AppendLine();
            sb.AppendLine("Respond in this **exact JSON format**, no explanations or commentary:");
            sb.AppendLine("{");
            sb.AppendLine("  \"intents\": [\"...\"],");
            sb.AppendLine("  \"entities\": [\"...\"],");
            sb.AppendLine("  \"dates\": [\"...\"],");
            sb.AppendLine("  \"sources\": [\"...\"]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"Query: {userQuery}");

            var systemPrompt = sb.ToString();

            var jsonResponse = await _openAiService.GetChatCompletionAsync(systemPrompt);

            try
            {
                var context = JsonSerializer.Deserialize<QueryIntentContext>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (context == null)
                {
                    throw new Exception("Deserialized context is null. Raw response: " + jsonResponse);
                }

                return context;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse model response as JSON. Raw content:\n" + jsonResponse, ex);
            }
        }


        //public async Task<QueryIntentContext> ExtractAsync(string userQuery)
        //{
        //    var systemPrompt = """
        //You are a smart assistant. Analyze the user query and extract:
        //- intents: high-level tasks (e.g., summarization, alert_filter, emotion_analysis, keyword_search)
        //- entities: key people, topics, or keywords mentioned
        //- dates: specific days or ranges relevant to the query
        //- sources: which data sources to use (alerts, transcripts, logs, email, etc.)

        //Respond in this JSON format:
        //{
        //  "intents": [...],
        //  "entities": [...],
        //  "dates": [...],
        //  "sources": [...]
        //}

        //Query: """ + userQuery;

        //    var jsonResponse = await _openAiService.GetChatCompletionAsync(systemPrompt);

        //    return JsonSerializer.Deserialize<QueryIntentContext>(jsonResponse);
        //}
    }

}
