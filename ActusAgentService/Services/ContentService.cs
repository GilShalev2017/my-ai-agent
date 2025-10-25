using ActusAgentService.DB;
using ActusAgentService.Models;
using ActusAgentService.Models.ActIntelligence;
using Microsoft.Extensions.AI;
using MongoDB.Driver;
using System.Text.Json;

namespace ActusAgentService.Services
{
    public interface IContentService
    {
        Task<List<JobResult>> GetFilteredTranscriptsAsync(JobResultFilter filter);
        //Task<List<string>> GetTranscriptsByTopicAndDateAsync(string userQuery, string topic, string date);
        Task<List<JobResult>> GetTranscriptsBySemanticSearchAndDateAsync(string userQuery, JobResultFilter filter);
        Task<List<string>>GetAlertsByDateAsync(string date);
    }

    public class ContentService : IContentService
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IAiJobResultRepositoryExtended _aiJobResultRepositoryExtended;
        private readonly IVectorDBRepository _vectorDbRepository;

        public ContentService(IEmbeddingProvider embeddingProvider, IAiJobResultRepositoryExtended aiJobResultRepositoryExtended, IVectorDBRepository vectorDBRepository)
        {
            _embeddingProvider = embeddingProvider;
            _aiJobResultRepositoryExtended = aiJobResultRepositoryExtended;
            _vectorDbRepository = vectorDBRepository;
        }

        public Task<List<string>> GetAlertsByDateAsync(string date)
        {
            throw new NotImplementedException();
        }

        public async Task<List<JobResult>> GetFilteredTranscriptsAsync(JobResultFilter filter)
        {
            return await _aiJobResultRepositoryExtended.GetFilteredTranscriptsAsync(filter);
        }

        public async Task<List<JobResult>> GetTranscriptsBySemanticSearchAndDateAsync(string userQuery, JobResultFilter filter)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(userQuery))
            {
                return new List<JobResult>();
            }

            try
            {
                // Step 1: Embed the user query to get a vector
                var queryVector = await _embeddingProvider.EmbedAsync(userQuery);

                // Step 2: Perform semantic search with date filtering in vector DB
                // The vector DB handles both semantic similarity and date filtering efficiently
                
                //var mongoIds = await _vectorDbRepository.SearchSimilarAsync(queryVector, filter, topK: 20);
                var mongoIds = await _vectorDbRepository.SearchSimilarAsyncWithDateTimeStrings(queryVector, filter, topK: 50); //if dates in the DB is string format
              
                var distinctMongoIds = mongoIds.Distinct().ToList();

                if (!distinctMongoIds.Any())
                {
                    return new List<JobResult>();
                }

                // Step 3: Fetch the full JobResult objects from the main database using the returned MongoIds
                var relevantJobTranscriptJobs = await _aiJobResultRepositoryExtended.GetJobResultsByIdsAsync(distinctMongoIds);

                // Handle case where some MongoIds might not exist in MongoDB (data consistency)
                if (!relevantJobTranscriptJobs.Any())
                {
                    return new List<JobResult>();
                }

                return relevantJobTranscriptJobs;

                // Step 4: Preserve semantic similarity order but group by chronological order within similar scores
                // Alternative: return relevantTranscripts.OrderBy(t => t.Start).ToList(); for pure chronological

                // Create a dictionary to preserve the original similarity order
                //var mongoIdOrder = mongoIds
                //    .Select((id, index) => new { Id = id, Order = index })
                //    .ToDictionary(x => x.Id, x => x.Order);

                //// Sort by similarity order first, then by start time for items with same similarity
                //return relevantTranscriptJobs
                //    .Where(t => mongoIdOrder.ContainsKey(t.Id)) // Ensure the transcript has a valid similarity score
                //    .OrderBy(t => mongoIdOrder.TryGetValue(t.Id, out var order) ? order : int.MaxValue)
                //    .ThenBy(t => t.Start)
                //    .ToList();
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to inject ILogger)
                // _logger?.LogError(ex, "Error during semantic search for query: {Query}", userQuery);

                // Fallback to regular filtered search without semantic similarity
                return await GetFilteredTranscriptsAsync(filter);
            }
        }
    }

}
