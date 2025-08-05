using ActusAgentService.Models;

namespace ActusAgentService.Services
{
    public interface IAgentDispatcher
    {
        Task<string> ExecuteAsync(QueryIntentContext context, QueryPlan plan, string llmResponse);
    }

    public class AgentDispatcher : IAgentDispatcher
    {
        public Task<string> ExecuteAsync(QueryIntentContext context, QueryPlan plan, string llmResponse)
        {
            var intent = plan.Intents.FirstOrDefault()?.ToLower() ?? "unknown";
            return Task.FromResult(intent switch
            {
                "summarization" => $"[SUMMARY] {llmResponse}",
                "emotion_analysis" => $"[EMOTION ANALYSIS] {llmResponse}",
                _ => $"[UNRECOGNIZED INTENT: {intent}]\n{llmResponse}"
            });
        }
    }

}