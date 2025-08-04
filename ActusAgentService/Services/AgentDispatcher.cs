namespace ActusAgentService.Services
{
    public class AgentDispatcher
    {
        public async Task<string> ExecuteAsync(QueryIntentContext context, QueryPlan plan, string llmResponse)
        {
            var intent = plan.Intents.FirstOrDefault() ?? "unknown";

            return intent switch
            {
                "summarization" => $"[SUMMARY of {plan.TranscriptLines.Count} transcripts]\n\n{llmResponse}",
                "emotion_analysis" => $"[EMOTION results on {plan.TranscriptLines.Count} transcripts]\n\n{llmResponse}",
                _ => "[Unsupported intent]"
            };
        }
    }

}