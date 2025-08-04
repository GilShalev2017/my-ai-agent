using ActusAgentService.Models;

namespace ActusAgentService.Services
{
    public class TranscriptSearchService
    {
        private readonly EmbeddingProvider _embeddingProvider;
        private readonly TranscriptRepository _repo;

        public TranscriptSearchService(EmbeddingProvider embeddingProvider, TranscriptRepository repo)
        {
            _embeddingProvider = embeddingProvider;
            _repo = repo;
        }

        public async Task<List<Transcript>> SemanticSearchAsync(string userQuery, int topK = 5)
        {
            var queryVec = await _embeddingProvider.EmbedTextAsync(userQuery);
        
            //var allTranscripts = await _repo.LoadAllTranscriptsAsync();

            //return allTranscripts
            //    .Select(t => new { Transcript = t, Score = CosineSimilarity(t.Embedding, queryVec) })
            //    .OrderByDescending(x => x.Score)
            //    .Take(topK)
            //    .Select(x => x.Transcript)
            //    .ToList();

            return [];
        }

        private float CosineSimilarity(float[] v1, float[] v2)
        {
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                normA += v1[i] * v1[i];
                normB += v2[i] * v2[i];
            }
            return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }

}
