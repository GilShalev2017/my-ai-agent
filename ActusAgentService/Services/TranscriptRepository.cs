using ActusAgentService.Models;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public class TranscriptRepository
    {
        //private readonly string _jsonPath;
        private readonly EmbeddingProvider _embeddingProvider;

        public TranscriptRepository(EmbeddingProvider embeddingProvider)
        {
            //_jsonPath = jsonPath;
            _embeddingProvider = embeddingProvider!;
        }

        //public async Task<List<Transcript>> LoadAllTranscriptsAsync()
        //{

        //    if (!File.Exists(_jsonPath)) return new List<Transcript>();

        //    var json = await File.ReadAllTextAsync(_jsonPath);

        //    return JsonSerializer.Deserialize<List<Transcript>>(json);
        //}

        public async Task<List<Transcript>> SearchTranscripts(string userQuery)
        {
            // Step 1: Embed the user query (using OpenAI Embedding API)
            var queryVector = await _embeddingProvider.EmbedAsync(userQuery);

            // Step 2: Search your transcript embeddings index (e.g., using Pinecone, Qdrant, or custom DB)
            
            // transcriptEmbeddingIndex.FindNearest(queryVector, topK: 10);
           
            return new List<Transcript>();
        }

        public async Task<List<string>> GetTranscriptsByTopicAndDateAsync(string userQuery, string topic, string date)
        {
            // Await the asynchronous operation.
            var queryVector = await _embeddingProvider.EmbedAsync(userQuery);

            // Simulate filtering by topic/date.
            // The C# compiler automatically wraps this List<string> in a Task<List<string>>
            // because the method is declared with 'async'.
            return new List<string> {
                $"Transcript about {topic} on {date}",
                $"Another {topic} segment on {date}"
            };
        }
    }

}
