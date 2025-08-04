using ActusAgentService.Services;
using Microsoft.Extensions.Hosting;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
//.AddJsonOptions(opts =>
//{
//    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
//}); 

builder.Services.AddSingleton<EmbeddingProvider>();
builder.Services.AddSingleton<TranscriptRepository>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<IntentDetector>();
builder.Services.AddSingleton<PlanGeneratorAgent>();
builder.Services.AddSingleton<AgentDispatcher>();
builder.Services.AddSingleton<EntityExtractor>();

builder.Services.AddHttpClient<OpenAiService>(client =>
{
    // The base address and headers are now configured once, at startup.
    client.BaseAddress = new Uri("https://api.openai.com/");

    // The API key is also retrieved from configuration here.
    // The header will be set automatically for all requests from this client.
    var apiKey = builder.Configuration.GetValue<string>("OpenAI:ApiKey");
 
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    
    client.Timeout = TimeSpan.FromMinutes(5);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();