using ActusAgentService.DB;
using ActusAgentService.Services;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
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

builder.Services.AddSingleton<IAiJobResultRepositoryExtended, AiJobResultRepositoryExtended>();
builder.Services.AddScoped<IEntityExtractor, EntityExtractor>();

//builder.Services.AddSingleton<IDateNormalizer, DateNormalizer>();

builder.Services.AddScoped<IEmbeddingProvider, EmbeddingProvider>();
builder.Services.AddScoped<IVectorDBRepository, VectorDBRepository>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IPlanGenerator, PlanGenerator>();
builder.Services.AddSingleton<IPromptComposer, PromptComposer>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IAgentDispatcher, AgentDispatcher>();

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

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDb");
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("YourDatabaseName"); // Replace with your DB name
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