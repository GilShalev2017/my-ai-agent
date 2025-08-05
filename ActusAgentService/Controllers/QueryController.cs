using ActusAgentService.Models;
using ActusAgentService.Services;
using Azure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Numerics;
using System.Text.Json;

namespace ActusAgentService.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]

    public class QueryController : ControllerBase
    {
        private readonly IEntityExtractor _entityExtractor;
        private readonly IDateNormalizer _dateNormalizer;
        private readonly IPlanGenerator _planGenerator;
        private readonly IPromptComposer _promptComposer;
        private readonly IOpenAiService _openAiService;
        private readonly IAgentDispatcher _agentDispatcher;

        public QueryController(IEntityExtractor entityExtractor,
                               IDateNormalizer dateNormalizer,
                               IPlanGenerator planGenerator,
                               IPromptComposer promptComposer,
                               IOpenAiService openAiService,
                               IAgentDispatcher agentDispatcher)
        {
            _entityExtractor = entityExtractor;
            _dateNormalizer = dateNormalizer;
            _planGenerator = planGenerator;
            _promptComposer = promptComposer;
            _agentDispatcher = agentDispatcher;
            _openAiService = openAiService;
        }
        
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string userQuery)
        {
            //extract → normalize → compose → execute

            QueryIntentContext context = await _entityExtractor.ExtractAsync(userQuery);

            Console.WriteLine($"Extracted: {JsonSerializer.Serialize(context)}");

            var plan = await _planGenerator.GeneratePlanAsync(context);

            string prompt = _promptComposer.Compose(context, plan);

            Console.WriteLine("Prompt:\n" + prompt);
            
            var response = await _openAiService.GetChatCompletionAsync(prompt);

            Console.WriteLine("Response:\n" + response);

            var result = await _agentDispatcher.ExecuteAsync(context, plan, response);

            return Ok(result);
        }


        private float Dot(float[] a, float[] b) => a.Zip(b, (x, y) => x * y).Sum();
    }

    //foreach (var intent in context.Intents)
    //{
    //    switch (intent.ToLowerInvariant())
    //    {
    //        case "summarization":
    //            // handle summarization
    //            break;
    //        case "emotion_analysis":
    //            // handle emotion analysis
    //            break;
    //        default:
    //            // Let GPT handle unknown intents as free-form prompts
    //            var prompt = BuildDynamicPrompt(intent, context.Entities, context.Dates);
    //    var answer = await _openAiService.GetChatCompletionAsync(prompt);
    //            return answer;
    //    }
    //}
}

//OPTION A WITH EMEDDING
/*
var transcripts = await _repo.LoadAllTranscriptsAsync();

var queryVec = await _embed.EmbedTextAsync(userQuery);

// Very naive cosine search
var results = transcripts
    .Where(t => t.Embedding != null)
    .Select(t => new { t, score = Dot(queryVec, t.Embedding) })
    .OrderByDescending(x => x.score)
    .Take(3)
    .Select(x => x.t.Text)
    .ToList();

var prompt = PromptBuilder.BuildPrompt(userQuery, results);

var answer = await _openAiService.GetChatCompletionAsync(prompt);

return Ok(answer);
*/


//OPTION B WITH Intent, Plan and Result - Working
/*
string intent = await _intentDetector.DetectIntentAsync(userQuery);

var plan = await _planGeneratorAgent.GeneratePlanAsync(userQuery, intent);

var result = await _agentDispatcher.ExecutePlanAsync(userQuery,plan);

return Ok(result);
*/

//OPTION C WITH EntityExtractor, PromptComposerContext and Result

/*
var extractor = new EntityExtractor(ai);

var context = await extractor.ExtractAsync(userQuery);

var composerContext = new PromptComposerContext
{
    UserQuery = userQuery,
    Intents = context.Intents,
    Entities = context.Entities,
    Dates = context.Dates,
    Sources = context.Sources,
    Alerts = await _alertRepo.FindAlerts(context.Entities, context.Dates),
    TranscriptLines = await _transcriptRepo.FindSegments(context.Entities, context.Dates),
};

var composer = new PromptComposer();

var prompt = composer.Compose(composerContext);

var result = await ai.GetChatCompletionAsync(prompt);
*/

/*
OPTION D WITH QueryPlanCache, EntityExtractor, DateNormalizer, PromptComposer and Result
// Step 1: Check cache
var hash = QueryPlanCache.GetHash(userQuery);
var cachedPlan = QueryPlanCache.Get(userQuery);
if (cachedPlan != null)
    return await _ai.GetChatCompletionAsync(cachedPlan.FinalPrompt);

// Step 2: Build new plan
var context = await extractor.ExtractAsync(userQuery);
var normalizedDates = normalizer.Normalize(context.Dates);

var composerContext = new PromptComposerContext
{
    UserQuery = userQuery,
    Intents = context.Intents,
    Entities = context.Entities,
    Dates = normalizedDates,
    Sources = context.Sources,
    Alerts = await _alertRepo.FindAlerts(context.Entities, normalizedDates),
    TranscriptLines = await _transcriptRepo.FindSegments(context.Entities, normalizedDates),
};

var prompt = new PromptComposer().Compose(composerContext);

// Step 3: Create and store plan
var plan = new QueryPlan
{
    QueryHash = hash,
    UserQuery = userQuery,
    Intents = context.Intents,
    Entities = context.Entities,
    Dates = normalizedDates,
    Sources = context.Sources,
    Alerts = composerContext.Alerts,
    TranscriptLines = composerContext.TranscriptLines,
    FinalPrompt = prompt
};

QueryPlanCache.Store(plan);

// Step 4: Execute prompt
var result = await _ai.GetChatCompletionAsync(prompt);
*/
