using ActusAgentService.Services;

namespace ActusAgentService.SemanticKernelIntegration
{
    public class TranscriptSkill
    {
        private readonly TranscriptSearchService _searchService;

        public TranscriptSkill(TranscriptSearchService searchService)
        {
            _searchService = searchService;
        }

        //[KernelFunction]
        //public async Task<string> FindRelevantTranscriptChunks(string query)
        //{
        //    var matches = await _searchService.SemanticSearchAsync(query);
        //    return string.Join("\n---\n", matches.Select(m => m.Text));
        //}
    }

}
