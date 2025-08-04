using ActusAgentService.Models;
using Microsoft.Extensions.AI;
using MongoDB.Driver;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public class TranscriptRepository
    {
        private readonly EmbeddingProvider _embeddingProvider;
        private readonly IMongoCollection<Transcript> _collection;

        public TranscriptRepository(EmbeddingProvider embeddingProvider, IMongoDatabase db)
        {
            _embeddingProvider = embeddingProvider;
            _collection = db.GetCollection<Transcript>("transcripts");
        }

        public async Task<List<string>> GetTranscriptsByTopicAndDateAsync(string userQuery, string topic, string date)
        {
            var queryVector = await _embeddingProvider.EmbedAsync(userQuery); // vector of floats

            var allDocs = await _collection
                .Find(d => d.Topic == topic && d.Date == date)
                .ToListAsync();

            return allDocs
                .Select(d => new {
                    text = d.Text,
                    similarity = CosineSimilarity(queryVector, d.Embedding)
                })
                .OrderByDescending(x => x.similarity)
                .Take(10)
                .Select(x => x.text)
                .ToList();
        }

        private float CosineSimilarity(float[] v1, float[] v2)
        {
            float dot = 0, mag1 = 0, mag2 = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }
            return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }
    }

}
