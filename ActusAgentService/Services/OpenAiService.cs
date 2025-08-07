using SharpToken;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public interface IOpenAiService
    {
        Task<string> GetChatCompletionAsync(string prompt, string data);
    }

    public class OpenAiService : IOpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "";
       // public const string TooManyTokensMarker = "__TOO_MANY_TOKENS__";

        public OpenAiService()
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            _apiKey = configuration.GetValue<string>("OpenAI:ApiKey");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        }

        public async Task<string> GetChatCompletionAsync(string prompt,string data)
        {
            var encoding = GptEncoding.GetEncodingForModel("gpt-4-turbo");
            int totalTokens = encoding.Encode(prompt).Count + encoding.Encode(data).Count;

            if (totalTokens > 128000)
            {
                //throw new Exception($"Your query contains too much data ({totalTokens} tokens). " +
                //    $"Please reduce the time range, number of channels, or amount of input data.");
                return $"__TOO_MANY_TOKENS__:{totalTokens}";
            }

            var body = new
            {
                model = "gpt-4-turbo",//Could be others!!!
                messages = new[]
                {
                     new { role = "system", content = prompt },  // Instructions and context
                     new { role = "user", content = data }    // The actual data to analyze
                     //new { role = "system", content = "You are a helpful media assistant." },
                     //new { role = "user", content = prompt }
                },
                //temperature = 0.2 // Adjust temperature for more deterministic output
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception("OpenAI API error: " + error);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

    }

}
