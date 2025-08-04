using Microsoft.Extensions.AI;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ActusAgentService.Services
{
    //A vector record example
    //id: You usually put the MongoDB document ID here(e.g., _id) so you can link it back later.

    //vector: The embedding generated using OpenAI or another model.
    //metadata: Optional — allows you to filter/search(e.g., by channel, date, type).
    //In MongoDB, you keep the full transcript text, metadata

    //{
    //  "id": "mongo_123456",
    //  "vector": [0.123, -0.234, 0.345, ...],
    //  "metadata": {
    //    "channelId": "CNN",
    //    "startTime": "2025-07-31T18:00:00Z"
    //  }
    //}

    public class EmbeddingProvider
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public EmbeddingProvider()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            _apiKey = configuration.GetValue<string>("OpenAI:ApiKey");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<float[]> EmbedAsync(string input)
        {
            //The input field in the embedding request can be:

            //A single string, or

            //An array of strings(up to 2048 entries for text - embedding - 3 - small).

            var request = new
            {
                input = input,
                model = "text-embedding-3-small" //text-embedding-ada-002
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/embeddings", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var embeddingArray = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return embeddingArray;
        }

        public async Task<float[]> EmbedTextAsync(string text)
        {
            // Simulated 3D vector (replace with OpenAI call)
            return new float[] { text.Length % 1f, 0.5f, 0.3f };
        }

        public async Task<float[]> GetEmbeddingAsync(string userQuery)
        {
            return [];

        }
    }

}
