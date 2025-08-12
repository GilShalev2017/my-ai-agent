using ActusAgentService.Models.ActIntelligence;
using System.Text.Json;
using ActInfra;

namespace ActusAgentService.Services
{
    public interface IVectorDBRepository
    {
        Task StoreTranscriptsAsync(List<TranscriptEx>? transcripts, bool includeTimestamps = false, string? mongoId = null);
        Task<IEnumerable<string>> SearchSimilarAsync(float[] queryVector, JobResultFilter dateFilter, int topK = 10);
    }

    public class VectorDBRepository : IVectorDBRepository
    {
        private readonly ILogger<VectorDBRepository> _logger;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly HttpClient _httpClient;
        private readonly string _collectionName;

        public VectorDBRepository(
            IConfigSettings config, // Make this configurable
            ILogger<VectorDBRepository> logger,
            IEmbeddingProvider embeddingProvider)
        {
            _logger = logger;
            _embeddingProvider = embeddingProvider;

            // Make these configurable via IConfigSettings
            var host = "localhost";// config.VectorDbHost ?? "localhost";
            var port = 6333;// config.VectorDbPort ?? 6333

            _collectionName = "transcripts";// config.VectorDbCollection ?? "transcripts";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{host}:{port}/"),
                Timeout = TimeSpan.FromSeconds(30) // Add timeout
            };
        }

        public async Task EnsureCollectionExistsAsync(int vectorSize = 1536)
        {
            try
            {
                var collectionUrl = $"collections/{_collectionName}";
                var response = await _httpClient.GetAsync(collectionUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var createResponse = await _httpClient.PutAsJsonAsync(collectionUrl, new
                    {
                        vectors = new
                        {
                            size = vectorSize,
                            distance = "Cosine"
                        }
                    });
                    createResponse.EnsureSuccessStatusCode();
                    _logger.LogInformation("Qdrant collection '{CollectionName}' created via REST.", _collectionName);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed to connect to Qdrant: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Collection creation failed or already exists: {Message}", ex.Message);
            }
        }

        public async Task StoreTranscriptsAsync(List<TranscriptEx>? transcripts, bool includeTimestamps = false, string? mongoId = null)
        {
            if (transcripts == null || transcripts.Count == 0)
            {
                _logger.LogWarning("No transcripts to store.");
                return;
            }

            if (string.IsNullOrEmpty(mongoId))
            {
                _logger.LogError("MongoDB ID is required for storing transcripts in vector database.");
                throw new ArgumentException("MongoDB ID cannot be null or empty.", nameof(mongoId));
            }

            await EnsureCollectionExistsAsync();
            var vector = await EmbedTranscripts(transcripts, includeTimestamps);

            // Store minimal payload - optimize storage
            var payload = new Dictionary<string, object>
            {
                ["mongo_id"] = mongoId
            };

            if (includeTimestamps)
            {
                payload["start"] = (double)transcripts.First().StartInSeconds;
                payload["end"] = (double)transcripts.Last().EndInSeconds;
            }

            // Optional: Add text for debugging (remove in production for storage optimization)
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                payload["text_preview"] = string.Join(" ", transcripts.Take(3).Select(t => t.Text));
            }

            var point = new
            {
                id = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), // ✅ Use ulong for Qdrant ID
                vector = vector,
                payload = payload
            };

            try
            {
                var requestObject = new { points = new[] { point } };

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestObject, jsonOptions);
                _logger.LogInformation("Qdrant request JSON:\n{Json}", json);
                Console.WriteLine($"Qdrant request JSON:\n{json}");

                // ✅ FIXED: Use PUT instead of POST for upsert operations
                var response = await _httpClient.PutAsync(
                    $"collections/{_collectionName}/points?wait=true",
                    new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                );

                var respBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Qdrant response: {Status} - {Body}", response.StatusCode, respBody);
                Console.WriteLine($"Qdrant response: {response.StatusCode} - {respBody}");

                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Stored embedding for {Count} transcript lines with MongoDB ID: {MongoId}",
                    transcripts.Count, mongoId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed to store embeddings in Qdrant: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<string>> SearchSimilarAsync(float[] queryVector, JobResultFilter dateFilter, int topK = 10)
        {
            var mustConditions = new List<object>();

            if (dateFilter.Start.HasValue)
            {
                mustConditions.Add(new
                {
                    key = "start",
                    range = new { gte = (double)(dateFilter.Start.Value.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds }
                });
            }

            if (dateFilter.End.HasValue)
            {
                mustConditions.Add(new
                {
                    key = "end",
                    range = new { lte = (double)(dateFilter.End.Value.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds }
                });
            }

            var filter = mustConditions.Any()
                ? new { must = mustConditions }
                : null;

            var requestBody = new
            {
                vector = queryVector,
                filter = filter,
                limit = topK,
                with_payload = true
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"collections/{_collectionName}/points/search", requestBody);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>();

                return result.RootElement.GetProperty("result").EnumerateArray()
                    .Select(item =>
                    {
                        var payload = item.GetProperty("payload");
                        if (payload.TryGetProperty("mongo_id", out var mongoIdProp))
                        {
                            return mongoIdProp.GetString();
                        }

                        _logger.LogWarning("Vector search result missing mongo_id in payload");
                        return null;
                    })
                    .Where(id => !string.IsNullOrEmpty(id))!;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Failed to search Qdrant: {Message}", ex.Message);
                throw;
            }
        }

        private async Task<float[]> EmbedTranscripts(List<TranscriptEx> transcriptExs, bool embedTimestamps)
        {
            if (!transcriptExs.Any())
                throw new ArgumentException("Transcript list is empty.");

            var combined = embedTimestamps
                ? string.Join(" ", transcriptExs.Select(t => $"{t.StartInSeconds}-{t.EndInSeconds}: {t.Text}"))
                : string.Join(" ", transcriptExs.Select(t => t.Text));

            return await _embeddingProvider.EmbedAsync(combined);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}