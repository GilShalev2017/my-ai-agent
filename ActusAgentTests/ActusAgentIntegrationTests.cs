using ActusAgentService.DB;
using ActusAgentService.Models;
using ActusAgentService.Services;
using Azure;
using Moq;
using System.Text.Json;

[TestClass]
public class ActusAgentIntegrationTests
{
    private readonly Mock<ContentService> _mockTranscriptRepo;
    private readonly EntityExtractor _entityExtractor;
    private readonly IOpenAiService _openAiService;
    private readonly IDateNormalizer _dateNormalizer;
    private readonly PlanGenerator _planGenerator;
    private readonly IContentService _contentService;

    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IAiJobResultRepositoryExtended _aiJobResultRepositoryExtended;
    private readonly PromptComposer _promptComposer;

    public ActusAgentIntegrationTests()
    {
        _mockTranscriptRepo = new Mock<ContentService>(null, null);
        _openAiService = new OpenAiService();
        _dateNormalizer = new DateNormalizer();
        _entityExtractor = new EntityExtractor(_openAiService, _dateNormalizer);
        _embeddingProvider = new EmbeddingProvider();
        _aiJobResultRepositoryExtended = new AiJobResultRepositoryExtended(null);
        _contentService = new ContentService(_embeddingProvider, _aiJobResultRepositoryExtended);
        _planGenerator = new PlanGenerator(_contentService);
        _promptComposer = new PromptComposer();
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

    [TestMethod]
    public async Task EntityExtractor_TestDates()
    {
        // Optional: check for consistency in repeated queries
        var query1 = "Was gil mentioned on the fifth of August?";
        var context1 = await _entityExtractor.ExtractAsync(query1);

        var query2 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
    }

    [TestMethod]
    public async Task EntityExtractor_TestExtractedDatesAndQueryPlans()
    {
        var query1 = "Was gil mentioned on the fifth of August?";
        var context1 = await _entityExtractor.ExtractAsync(query1);
        QueryPlan plan1 = await _planGenerator.GeneratePlanAsync(context1);
        Console.WriteLine("\nQuery 1 Dates: " + string.Join(", ", plan1.Dates));
        foreach (var date in plan1.RawDates ?? new())
        {
            Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        }
        Console.WriteLine("Query 1 TranscriptLines Count: " + plan1.TranscriptLines.Count);
        Console.WriteLine($"Query 1 Filter: Operation={plan1.Filter?.Operation}, Start={plan1.Filter?.Start}, End={plan1.Filter?.End}");

        var query2 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
        QueryPlan plan2 = await _planGenerator.GeneratePlanAsync(context2);
        Console.WriteLine("\nQuery 2 Dates: " + string.Join(", ", plan2.Dates));
        foreach (var date in plan2.RawDates ?? new())
        {
            Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        }
        Console.WriteLine("Query 2 TranscriptLines Count: " + plan2.TranscriptLines.Count);
        Console.WriteLine($"Query 2 Filter: Operation={plan2.Filter?.Operation}, Start={plan2.Filter?.Start}, End={plan2.Filter?.End}");

        var query3 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        var context3 = await _entityExtractor.ExtractAsync(query3);
        QueryPlan plan3 = await _planGenerator.GeneratePlanAsync(context3);
        Console.WriteLine("\nQuery 3 Dates: " + string.Join(", ", plan3.Dates));
        foreach (var date in plan3.RawDates ?? new())
        {
            Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        }
        Console.WriteLine("Query 3 TranscriptLines Count: " + plan3.TranscriptLines.Count);
        Console.WriteLine($"Query 3 Filter: Operation={plan3.Filter?.Operation}, Start={plan3.Filter?.Start}, End={plan3.Filter?.End}");
    }

    [TestMethod]
    public async Task EntityExtractor_TestExtractedDatesQueryPlansAndPrompts()
    {
        var query1 = "Was gil mentioned on the fifth of August?";
        var context1 = await _entityExtractor.ExtractAsync(query1);
        QueryPlan plan1 = await _planGenerator.GeneratePlanAsync(context1);
        //Console.WriteLine("\nQuery 1 Dates: " + string.Join(", ", plan1.Dates));
        //foreach (var date in plan1.RawDates ?? new())
        //{
        //    Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        //}
        Console.WriteLine("Query 1 TranscriptLines Count: " + plan1.TranscriptLines.Count);
        Console.WriteLine($"Query 1 Filter: Operation={plan1.Filter?.Operation}, Start={plan1.Filter?.Start}, End={plan1.Filter?.End}");
        (string systemMessage, string data) = _promptComposer.Compose(context1, plan1);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }

        var query2 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
        QueryPlan plan2 = await _planGenerator.GeneratePlanAsync(context2);
        //Console.WriteLine("\nQuery 2 Dates: " + string.Join(", ", plan2.Dates));
        //foreach (var date in plan2.RawDates ?? new())
        //{
        //    Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        //}
        Console.WriteLine("Query 2 TranscriptLines Count: " + plan2.TranscriptLines.Count);
        Console.WriteLine($"Query 2 Filter: Operation={plan2.Filter?.Operation}, Start={plan2.Filter?.Start}, End={plan2.Filter?.End}");
        (systemMessage, data) = _promptComposer.Compose(context2, plan2);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }

        var query3 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        var context3 = await _entityExtractor.ExtractAsync(query3);
        Console.WriteLine("\nRaw Json Response: " + context3.RawJsonResponse);
        QueryPlan plan3 = await _planGenerator.GeneratePlanAsync(context3);
        //Console.WriteLine("\nQuery 3 Dates: " + string.Join(", ", plan3.Dates));
        //foreach (var date in plan3.RawDates ?? new())
        //{
        //    Console.WriteLine($"  Date: {date.Date}, StartTime: {date.StartTime}, EndTime: {date.EndTime}, Type: {date.Type}");
        //}
        Console.WriteLine("Query 3 TranscriptLines Count: " + plan3.TranscriptLines.Count);
        Console.WriteLine($"Query 3 Filter: Operation={plan3.Filter?.Operation}, Start={plan3.Filter?.Start}, End={plan3.Filter?.End}");
        (systemMessage, data) = _promptComposer.Compose(context3, plan3);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }
    }

    [TestMethod]
    public async Task EntityExtractor_TestRawJsonResponseOfContext()
    {
        //var query1 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        //var context1 = await _entityExtractor.ExtractAsync(query1);
        //Console.WriteLine("\nRaw Json Response: " + context1.RawJsonResponse);

        //var query2 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        //var context2 = await _entityExtractor.ExtractAsync(query2);
        //Console.WriteLine("\nRaw Json Response: " + context2.RawJsonResponse);

        //var query3 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        //var context3 = await _entityExtractor.ExtractAsync(query3);
        //Console.WriteLine("\nRaw Json Response: " + context3.RawJsonResponse);


        //var query1 = "Was gil mentioned on the fifth of August?";
        //var context1 = await _entityExtractor.ExtractAsync(query1);
        //Console.WriteLine("\nRaw Json Response: " + context1.RawJsonResponse);

        //var query2 = "Was gil mentioned on the fifth of August?";
        //var context2 = await _entityExtractor.ExtractAsync(query2);
        //Console.WriteLine("\nRaw Json Response: " + context2.RawJsonResponse);

        //var query3 = "Was gil mentioned on the fifth of August?";
        //var context3 = await _entityExtractor.ExtractAsync(query3);
        //Console.WriteLine("\nRaw Json Response: " + context3.RawJsonResponse);

        var query1 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context1 = await _entityExtractor.ExtractAsync(query1);
        Console.WriteLine("\nRaw Json Response: " + context1.RawJsonResponse);

        var query2 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
        Console.WriteLine("\nRaw Json Response: " + context2.RawJsonResponse);

        var query3 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context3 = await _entityExtractor.ExtractAsync(query3);
        Console.WriteLine("\nRaw Json Response: " + context3.RawJsonResponse);
    }

    [TestMethod]
    public async Task EntityExtractor_ExecuteChatCompletion()
    {
        var query1 = "Was gil mentioned on the fifth of August?";
        var context1 = await _entityExtractor.ExtractAsync(query1);
        QueryPlan plan1 = await _planGenerator.GeneratePlanAsync(context1);
        Console.WriteLine("Query 1 TranscriptLines Count: " + plan1.TranscriptLines.Count);
        Console.WriteLine($"Query 1 Filter: Operation={plan1.Filter?.Operation}, Start={plan1.Filter?.Start}, End={plan1.Filter?.End}");
        (string systemMessage, string data) = _promptComposer.Compose(context1, plan1);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }
        var response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        Console.WriteLine("Response:\n" + response);

        var query2 = "Was gil mentioned on the fifth of August between 20:00 and 22:00 in the attached transcripts?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
        QueryPlan plan2 = await _planGenerator.GeneratePlanAsync(context2);
        Console.WriteLine("Query 2 TranscriptLines Count: " + plan2.TranscriptLines.Count);
        Console.WriteLine($"Query 2 Filter: Operation={plan2.Filter?.Operation}, Start={plan2.Filter?.Start}, End={plan2.Filter?.End}");
        (systemMessage, data) = _promptComposer.Compose(context2, plan2);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }
        response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        Console.WriteLine("Response:\n" + response);

        var query3 = "Was gil mentioned between the fifth of August 20:00 to the sixth of August same hour, in the attached transcripts?";
        var context3 = await _entityExtractor.ExtractAsync(query3);
        QueryPlan plan3 = await _planGenerator.GeneratePlanAsync(context3);
        Console.WriteLine("Query 3 TranscriptLines Count: " + plan3.TranscriptLines.Count);
        Console.WriteLine($"Query 3 Filter: Operation={plan3.Filter?.Operation}, Start={plan3.Filter?.Start}, End={plan3.Filter?.End}");
        (systemMessage, data) = _promptComposer.Compose(context3, plan3);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }
        response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        Console.WriteLine("Response:\n" + response);
    }

    [TestMethod]
    public async Task ExecuteChatGPT_ExpectResults()
    {
        //var query3 = "Were Netanyahu, Macron, or Trump mentioned on August 5th between 20:00 and 22:00 in the attached transcripts?";
        //var context3 = await _entityExtractor.ExtractAsync(query3);
        //QueryPlan plan3 = await _planGenerator.GeneratePlanAsync(context3);
        //Console.WriteLine("Query 3 TranscriptLines Count: " + plan3.TranscriptLines.Count);
        //Console.WriteLine($"Query 3 Filter: Operation={plan3.Filter?.Operation}, Start={plan3.Filter?.Start}, End={plan3.Filter?.End}");
        //(var systemMessage, var data) = _promptComposer.Compose(context3, plan3);
        //Console.WriteLine("SystemMessage:\n" + systemMessage);
        //if (string.IsNullOrWhiteSpace(data))
        //{
        //    Console.WriteLine("No data available for this query.");
        //}
        //var response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        //Console.WriteLine("Response:\n" + response);


        //var query1 = "Were Gaza, Trump, Putin, Netanyahu or Macron mentioned in the attached transcripts, on August 6th between 18:00 and 18:30 ?";
        //var context1 = await _entityExtractor.ExtractAsync(query1);
        //QueryPlan plan1 = await _planGenerator.GeneratePlanAsync(context1);
        //Console.WriteLine("Query 1 TranscriptLines Count: " + plan1.TranscriptLines.Count);
        //Console.WriteLine($"Query 1 Filter: Operation={plan1.Filter?.Operation}, Start={plan1.Filter?.Start}, End={plan1.Filter?.End}");
        //(var systemMessage, var data) = _promptComposer.Compose(context1, plan1);
        //Console.WriteLine("SystemMessage:\n" + systemMessage);
        //if (string.IsNullOrWhiteSpace(data))
        //{
        //    Console.WriteLine("No data available for this query.");
        //}
        //var response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        //Console.WriteLine("Response:\n" + response);


        var query2 = "Can you summarize the main keypoints delivered in the attached transcripts between 19:00 and 19:30 on August 6th ?";
        var context2 = await _entityExtractor.ExtractAsync(query2);
        QueryPlan plan2 = await _planGenerator.GeneratePlanAsync(context2);
        Console.WriteLine("Query 2 TranscriptLines Count: " + plan2.TranscriptLines.Count);
        Console.WriteLine($"Query 2 Filter: Operation={plan2.Filter?.Operation}, Start={plan2.Filter?.Start}, End={plan2.Filter?.End}");
        (var systemMessage, var data) = _promptComposer.Compose(context2, plan2);
        Console.WriteLine("SystemMessage:\n" + systemMessage);
        if (string.IsNullOrWhiteSpace(data))
        {
            Console.WriteLine("No data available for this query.");
        }
        var response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
        Console.WriteLine("Response:\n" + response);
    }
}