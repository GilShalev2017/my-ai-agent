namespace ActusAgentService.Services
{
    public class AgentDispatcher
    {
        private readonly TranscriptRepository _repo;

        public AgentDispatcher(TranscriptRepository repo, EmbeddingProvider embeddingProvider) {
            _repo = repo;
        }

        public async Task<string> ExecutePlanAsync(string userQuery, Dictionary<string, object> plan)
        {
            var intent = plan["intent"]?.ToString();
            var topic = plan["topic"]?.ToString() ?? "";
            var date = plan["date"]?.ToString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            var data = await _repo.GetTranscriptsByTopicAndDateAsync(userQuery, topic, date);
            
            //SemanticSearchAsync

            return intent switch
            {
                "summarization" => $"[SUMMARY of {data.Count} transcripts]",
                "emotion_analysis" => $"[EMOTION results on {data.Count} transcripts]",
                _ => "[Unsupported intent]"
            };
        }
    }

}
