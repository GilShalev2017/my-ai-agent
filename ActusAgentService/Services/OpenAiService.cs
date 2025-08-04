using System.Text.Json;

namespace ActusAgentService.Services
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "";

        public OpenAiService(HttpClient httpClient)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            _apiKey = configuration.GetValue<string>("OpenAI:ApiKey");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        }

        public async Task<string> GetChatCompletionAsync(string prompt)
        {
            var body = new
            {
                model = "gpt-4",
                messages = new[] {
                    new { role = "system", content = "You are an assistant that analyzes TV transcript user queries." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API call failed: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

    }

}
