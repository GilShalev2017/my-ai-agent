using ActusAgentService.Models;
using ActusAgentService.Models.ActIntelligence;
using ActusAgentService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static MongoDB.Driver.WriteConcern;

namespace ActusAgentService.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]

    public class QueryController : ControllerBase
    {
        private readonly IEntityExtractor _entityExtractor;
        private readonly IPlanGenerator _planGenerator;
        private readonly IPromptComposer _promptComposer;
        private readonly IOpenAiService _openAiService;
        private readonly IAgentDispatcher _agentDispatcher;

        private readonly IContentService _contentService;

        public QueryController(IEntityExtractor entityExtractor,
                               IDateNormalizer dateNormalizer,
                               IPlanGenerator planGenerator,
                               IPromptComposer promptComposer,
                               IOpenAiService openAiService,
                               IAgentDispatcher agentDispatcher,
                               IContentService contentService)
        {
            _entityExtractor = entityExtractor;
            _planGenerator = planGenerator;
            _promptComposer = promptComposer;
            _agentDispatcher = agentDispatcher;
            _openAiService = openAiService;

            _contentService = contentService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string userQuery)
        {
            //extract →  prepare data (plan) →  compose → execute

            try
            {
                QueryIntentContext context = await _entityExtractor.ExtractAsync(userQuery);

                Console.WriteLine($"Extracted: {JsonSerializer.Serialize(context)}");

                QueryPlan plan = await _planGenerator.GeneratePlanAsync(context);

                var (systemMessage, data) = _promptComposer.Compose(context, plan);

                Console.WriteLine("SystemMessage:\n" + systemMessage);

                // Handle empty data gracefully
                if (string.IsNullOrWhiteSpace(data))
                {
                    return Ok(new
                    {
                        message = "I couldn’t find any relevant data to answer your question. Please try rephrasing or ask about a different topic."
                    });
                }

                var response = await _openAiService.GetChatCompletionAsync(systemMessage, data);
                //or
                //var response = await _openAiService.GetChatCompletionAsync(userQuery, data);

                if (response.StartsWith("__TOO_MANY_TOKENS__"))
                {
                    int totalTokens = int.Parse(response.Split(':')[1]);
                    return Ok(new
                    {
                        message = $"The query is too large to process ({totalTokens} tokens). Please reduce the time range, number of channels, or amount of input data and try again."
                    });
                }

                Console.WriteLine("Response:\n" + response);

                var result = await _agentDispatcher.ExecuteAsync(context, plan, response);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("agent-transcripts")]
        public async Task<List<JobResult>> GetFilteredJobRequests([FromBody] JobResultFilter filter)
        {
            return await _contentService.GetFilteredTranscriptsAsync(filter);
        }
    }
}
//OPTION A WITH EMEDDING
/*
var transcripts = await _contentService.LoadAllTranscriptsAsync();

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
