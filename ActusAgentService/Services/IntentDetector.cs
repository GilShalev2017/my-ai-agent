namespace ActusAgentService.Services
{
    //public class IntentDetector
    //{
    //    private readonly OpenAiService _ai;

    //    public IntentDetector(OpenAiService ai)
    //    {
    //        _ai = ai;
    //    }

    //    public async Task<string> DetectIntentAsync(string query)
    //    {
    //        var prompt = $"Classify the intent of this user query: \"{query}\".\nPossible intents: summarization, emotion_analysis, keyword_search, alert_filter.\nReturn just the intent keyword.";
    //        //var prompt = $"Classify the intent(s) of this user query: \"{query}\".\nPossible intents: summarization, emotion_analysis, keyword_search, alert_filter, face_detection, entity_search, cross_reference, log_analysis.\nReturn a comma-separated list of relevant intent keywords.";

    //        var intent = await _ai.GetChatCompletionAsync(prompt);
            
    //        return intent.Trim().ToLower();
    //    }
    //}
}
