using ActusAgentService.Models;
using ActusAgentService.Services;
using Moq;
using System.Text.Json;

[TestClass]
public class ActusAgentIntegrationTests
{
    private readonly Mock<ContentService> _mockTranscriptRepo;
    private readonly EntityExtractor _entityExtractor;
    private readonly IOpenAiService _openAiService;
    private readonly IDateNormalizer _dateNormalizer;

    public ActusAgentIntegrationTests()
    {
        _mockTranscriptRepo = new Mock<ContentService>(null, null);
        _openAiService = new OpenAiService();
        _dateNormalizer = new DateNormalizer();
        _entityExtractor = new EntityExtractor(_openAiService, _dateNormalizer);
    }

    [TestMethod]
    public async Task EntityExtractor_ShouldHandleMultipleQueriesAndPrintResults()
    {
        /*
        var userQueries = new[]
        {
            "Was violence discussed yesterday in any of the channels?",
            "Summarize all shows that discussed Trump's peace efforts",
            "Find the top 3 suspicious clips from yesterday and email me the alerts.",
            "Alert me if anything emotional and politically sensitive happened on CNN today",
            "Show me all alerts from CNN yesterday",
            "What were the most controversial things said yesterday on the sports channels?",
            "Were there any angry reactions to the Prime Minister speech on news channels last night?",
            "Summarize all CNN segments on July 30th where alerts were triggered and show their emotional tone.",
            "Summarize all coverage of Netanyahu in the alerts and transcripts from July 30th.",
            "Give me a summary of all security alerts from last week about phishing emails",
            "Show me all meetings with John from last week about budget.",
        };

        var results = new Dictionary<string, QueryIntentContext>();

        foreach (var query in userQueries)
        {
            Console.WriteLine($"\n==== Query: {query} ====");
            try
            {
                var context = await _entityExtractor.ExtractAsync(query);
                results[query] = context;

                PrintContext(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting query: {query}\nException: {ex.Message}");
            }
        }
        */

        // Optional: check for consistency in repeated queries
        var repeatQuery = "What were the most controversial things said yesterday on the sports channels?";
        var repeat1 = await _entityExtractor.ExtractAsync(repeatQuery);
        var repeat2 = await _entityExtractor.ExtractAsync(repeatQuery);

        Console.WriteLine($"\n=== REPEATED QUERY TEST ===");
        Console.WriteLine($"First Run:\n{JsonSerializer.Serialize(repeat1, new JsonSerializerOptions { WriteIndented = true })}");
        Console.WriteLine($"Second Run:\n{JsonSerializer.Serialize(repeat2, new JsonSerializerOptions { WriteIndented = true })}");

        
        //Assert.AreEqual(
        //    JsonSerializer.Serialize(repeat1),
        //    JsonSerializer.Serialize(repeat2),
        //    "Repeated queries should return the same result (may vary slightly due to temperature)"
        //);


        Assert.AreEqual(repeat1.OriginalQuery, repeat2.OriginalQuery);
        //CollectionAssert.AreEqual(repeat1.Intents, repeat2.Intents);

        // For entities/sources/dates, compare content-wise
        //Assert.IsTrue(AreEntitiesEqual(repeat1.Intents, repeat2.Intents));

        // Fix for the errors
        Assert.IsTrue(AreSourcesEqual(repeat1.Sources, repeat2.Sources));
        Assert.IsTrue(AreEntitiesEqual(repeat1.Entities, repeat2.Entities));
        Assert.IsTrue(AreDatesEqual(repeat1.Dates, repeat2.Dates));
    }

    private void PrintContext(QueryIntentContext context)
    {
        Console.WriteLine($"Original Query: {context.OriginalQuery}");
        Console.WriteLine($"Intents: {string.Join(", ", context.Intents ?? new List<string>())}");

        Console.WriteLine("Entities:");
        if (context.Entities != null)
        {
            foreach (var e in context.Entities)
                Console.WriteLine($"  - [{e.Type}] {e.EntityName}");
        }

        Console.WriteLine("Dates: " + string.Join(", ", context.Dates?.Select(d => d.Date) ?? new List<string>()));
        Console.WriteLine("Sources: " + string.Join(", ", context.Sources?.Select(s => s.Source) ?? new List<string>()));
    }

    private bool AreEntitiesEqual(List<Entity> a, List<Entity> b)
    {
        if (a.Count != b.Count) return false;

        return a.Zip(b).All(pair => pair.First.EntityName == pair.Second.EntityName &&
                                    pair.First.Type == pair.Second.Type);
    }

    private bool AreDatesEqual(List<DateEntity> a, List<DateEntity> b)
    {
        if (a.Count != b.Count) return false;

        return a.Zip(b).All(pair => pair.First.Date == pair.Second.Date &&
                                    pair.First.Type == pair.Second.Type);
    }
    private bool AreSourcesEqual(List<SourceEntity> a, List<SourceEntity> b)
    {
        if (a.Count != b.Count) return false;

        return a.Zip(b).All(pair => pair.First.Source == pair.Second.Source &&
                                    pair.First.Type == pair.Second.Type);
    }

}
