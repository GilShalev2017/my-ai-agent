using System.Text.Json;

namespace ActusAgentService.Services
{
    public class PlanGeneratorAgent
    {
        private readonly OpenAiService _ai;

        public PlanGeneratorAgent(OpenAiService ai)
        { 
            _ai = ai; 
        }

        public async Task<Dictionary<string, object>> GeneratePlanAsync(string query, string intent)
        {
            // Use $$""" to correctly handle the braces in the JSON.
            // The interpolation placeholders now require three braces: {{{...}}}
            var prompt = $$"""
                        You are a planning agent.
                        Given the user query below, return a structured plan as JSON.
        
                        Intent: {{{intent}}}
                        Query: "{{{query}}}"

                        Respond in this format:
                        {
                            "intent": "emotion_analysis",
                            "topic": "...",
                            "channels": [...],
                            "date": "...",
                            "agents": ["EmotionDetectionAgent"],
                            "result_type": "summary"
                        }
                        """;

            var json = await _ai.GetChatCompletionAsync(prompt);
          
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
    }

}
