using ActusAgentService.Models;
using ActusAgentService.Models.ActIntelligence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace ActusAgentService.DB
{
    public class AIDetection
    {
        public string? JobRequestId { get; set; }
        public int ChannelId { get; set; }
        public string ChannelDisplayName { get; set; } = "";
        public string Operation { get; set; } = "";
        /// <summary>
        /// mongo will save this as UTC anyway, but when we ge the document from mongo the datetime will be converted to localtime.
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime End { get; set; }
        public string Text { get; set; } = "";
        public string? Keyword { get; set; } = "";
    }

    public interface IAiJobResultRepositoryExtended
    {
        Task UpdateJobResultAsync(JobResult jobResult);
        Task<JobResult> GetJobResultByIdAsync(string jobResultId);
        Task<string> SaveJobResultAsync(JobResult jobResult);
        Task<List<AIDetection>> GetFilteredAIDetectionsAsync(JobResultFilter filter);
        Task<List<string>?> GetDistinctAIDetectedKeywordsAsync(JobResultFilter filter);
        Task<bool> DeleteJobRelatedResultsAsync(string jobId);

        Task<bool> DeleteResultsOlderThanDateAsync(DateTime localDateTime);
        public Task<List<FaceDetectionResult>> GetFilteredFaces(FaceDetectionFilter faceDetectionFilter);
        public Task<List<FaceDetectionResult>> GetFilteredObjects(FaceDetectionFilter faceDetectionFilter);
        public Task<DateTime> GetMostRecentFaceTimestampAsync(int channelId);
        public Task<List<JobResult>> GetFilteredTranscriptsAsync(JobResultFilter filter);
        public Task<List<JobResult>> GetJobResultsByIdsAsync(IEnumerable<string> mongoIds);
    }
    public class AiJobResultRepositoryExtended : IAiJobResultRepositoryExtended
    {
        const string CollectionName = "intelligence_aijob_results";
        private readonly ILogger<AiJobResultRepositoryExtended> _logger;
        private readonly IMongoCollection<JobResult> _aiJobResultCollection;
        private readonly IMongoCollection<BsonDocument> _bsonCollection;
        private readonly int _mongoItemsLimit = 500;
        public AiJobResultRepositoryExtended(ILogger<AiJobResultRepositoryExtended> logger)
        {
            var mongoClient = new MongoClient("mongodb://localhost:27017");
            var mongoDatabase = mongoClient.GetDatabase("ActusIntegration");
            _aiJobResultCollection = mongoDatabase.GetCollection<JobResult>(CollectionName);

            _bsonCollection = mongoDatabase.GetCollection<BsonDocument>(CollectionName);
            _logger = logger;
        }
        public async Task<string> SaveJobResultAsync(JobResult jobResult)
        {
            await _aiJobResultCollection.InsertOneAsync(jobResult);
            return jobResult!.Id!; // Assuming Id is generated during insertion
        }
        public async Task<JobResult> GetJobResultByIdAsync(string jobResultId)
        {
            // Assuming you're using an ORM like Entity Framework or MongoDB
            // For example, with MongoDB:
            var jobResult = await _aiJobResultCollection
                                                  .Find(result => result.Id == jobResultId)
                                                  .FirstOrDefaultAsync();

            return jobResult;
        }
        public async Task UpdateJobResultAsync(JobResult jobResult)
        {
            if (jobResult == null)
            {
                throw new ArgumentNullException(nameof(jobResult), "JobResult cannot be null");
            }

            // Assuming you're using MongoDB:
            var updateDefinition = Builders<JobResult>.Update.Set(result => result.AiJobRequestId, jobResult.AiJobRequestId);
            await _aiJobResultCollection.UpdateOneAsync(result => result.Id == jobResult.Id, updateDefinition);
        }
        public async Task<List<JobResult>> GetFilteredTranscriptsAsync(JobResultFilter filter)
        {
            var filterStart = filter.Start;
            var filterEnd = filter.End;

            // Step 2: Build the dynamic filter for JobResults
            var jobFilters = new List<FilterDefinition<JobResult>>();

            // Add time range filters
            if (filterStart.HasValue)
                jobFilters.Add(Builders<JobResult>.Filter.Gte(jr => jr.End, filterStart.Value));

            if (filterEnd.HasValue)
                jobFilters.Add(Builders<JobResult>.Filter.Lte(jr => jr.Start, filterEnd.Value));

            // Add ChannelIds filter
            if (filter.ChannelIds != null && filter.ChannelIds.Length > 0)
                jobFilters.Add(Builders<JobResult>.Filter.In(jr => jr.ChannelId, filter.ChannelIds));

            // Add Operation filter
            if (!string.IsNullOrEmpty(filter.Operation))
                jobFilters.Add(Builders<JobResult>.Filter.Eq(jr => jr.Operation, filter.Operation));

            if (!string.IsNullOrEmpty(filter.AiJobRequestId))
                jobFilters.Add(Builders<JobResult>.Filter.Eq("AiJobRequestId", filter.AiJobRequestId));

            // Combine all filters using AND
            var combinedFilter = jobFilters.Any()
                ? Builders<JobResult>.Filter.And(jobFilters)
                : FilterDefinition<JobResult>.Empty;

            // Step 3: Fetch the filtered JobResults
            var jobResults = await _aiJobResultCollection.Find(combinedFilter).ToListAsync();

            return jobResults;
        }

        public async Task<List<AIDetection>> GetFilteredAIDetectionsAsync(JobResultFilter filter)
        {
            var filterStart = filter.Start;
            var filterEnd = filter.End;

            // Step 2: Build the dynamic filter for JobResults
            var jobFilters = new List<FilterDefinition<JobResult>>();

            // Add time range filters
            if (filterStart.HasValue)
                jobFilters.Add(Builders<JobResult>.Filter.Gte(jr => jr.End, filterStart.Value));

            if (filterEnd.HasValue)
                jobFilters.Add(Builders<JobResult>.Filter.Lte(jr => jr.Start, filterEnd.Value));

            // Add ChannelIds filter
            if (filter.ChannelIds != null && filter.ChannelIds.Length > 0)
                jobFilters.Add(Builders<JobResult>.Filter.In(jr => jr.ChannelId, filter.ChannelIds));

            // Add Operation filter
            if (!string.IsNullOrEmpty(filter.Operation))
                jobFilters.Add(Builders<JobResult>.Filter.Eq(jr => jr.Operation, filter.Operation));

            if (!string.IsNullOrEmpty(filter.AiJobRequestId))
                jobFilters.Add(Builders<JobResult>.Filter.Eq("AiJobRequestId", filter.AiJobRequestId));

            // Combine all filters using AND
            var combinedFilter = jobFilters.Any()
                ? Builders<JobResult>.Filter.And(jobFilters)
                : FilterDefinition<JobResult>.Empty;

            // Step 3: Fetch the filtered JobResults
            var jobResults = await _aiJobResultCollection.Find(combinedFilter).ToListAsync();

            // Step 4: Extract and filter events from the Transcript list
            var events = jobResults
                .SelectMany(jr => jr.Content ?? new List<TranscriptEx>(), (jr, transcript) => new AIDetection
                {
                    ChannelId = jr.ChannelId,
                    ChannelDisplayName = jr.ChannelDisplayName,
                    Operation = jr.Operation,
                    Start = transcript.StartTime,
                    End = transcript.EndTime,
                    Text = transcript.Text,
                    Keyword = transcript.Keyword,
                    JobRequestId = jr.AiJobRequestId,
                })
                .Where(e =>
                    (!filterStart.HasValue || e.Start >= filterStart.Value) &&
                    (!filterEnd.HasValue || e.End <= filterEnd.Value) &&
                    (filter.Keywords == null || filter.Keywords.Length == 0 || // No keyword filtering if Keywords is empty
                     (!string.IsNullOrEmpty(e.Text) &&
                      filter.Keywords.Any(kw => e.Text.Contains(kw, StringComparison.OrdinalIgnoreCase))))) // Match any keyword
                .ToList();

            // Step 5: Sort based on SortDirection
            if (filter.SortDirection == 1)
            {
                // Sort by oldest to newest
                events = events.OrderBy(e => e.Start).ToList();
            }
            else if (filter.SortDirection == 0)
            {
                // Sort by newest to oldest
                events = events.OrderByDescending(e => e.Start).ToList();
            }

            return events;
        }
        public async Task<List<string>?> GetDistinctAIDetectedKeywordsAsync(JobResultFilter filter)
        {
            // Step 1: Get all filtered alerts using the existing method
            List<AIDetection> filteredAIDetections = await GetFilteredAIDetectionsAsync(filter);

            // Step 2: Extract distinct keywords
            List<string>? distinctKeywords = filteredAIDetections
                .Where(alert => !string.IsNullOrEmpty(alert.Keyword)) // Ensure keyword is not null or empty
                .Select(alert => alert.Keyword!) // Use null-forgiving operator
                .Distinct() // Get unique keywords
                .ToList();

            return distinctKeywords;
        }
        public async Task<bool> DeleteJobRelatedResultsAsync(string aiJobRequestId)
        {
            if (string.IsNullOrEmpty(aiJobRequestId))
            {
                throw new ArgumentException("AiJobRequestId cannot be null or empty.", nameof(aiJobRequestId));
            }

            var filter = Builders<JobResult>.Filter.Eq(jr => jr.AiJobRequestId, aiJobRequestId);

            var result = await _aiJobResultCollection.DeleteManyAsync(filter);

            return result.DeletedCount > 0; // Return true if any documents were deleted, otherwise false
        }

        public async Task<bool> DeleteResultsOlderThanDateAsync(DateTime localDateTime)
        {
            var filter = Builders<JobResult>.Filter.Lte(jr => jr.End, localDateTime);

            var result = await _aiJobResultCollection.DeleteManyAsync(filter);

            return result.DeletedCount > 0;
        }

        public async Task<DateTime> GetMostRecentFaceTimestampAsync(int channelId)
        {
            // Step 1: Build the filter for the specified channelId
            var filter = Builders<JobResult>.Filter.Eq(jr => jr.ChannelId, channelId);

            // Step 2: Query the database for the most recent JobResult for the channel
            var mostRecentJobResult = await _aiJobResultCollection
                .Find(filter)
                .SortByDescending(jr => jr.End) // Sort by the most recent JobResult
                .Limit(1) // Only fetch the most recent JobResult
                .FirstOrDefaultAsync();

            // Step 3: Check if a JobResult was found
            if (mostRecentJobResult == null || mostRecentJobResult.Faces == null || !mostRecentJobResult.Faces.Any())
            {
                return DateTime.MinValue; // No faces found for the given channelId
            }

            // Step 4: Find the most recent face.TimestampStart in the JobResult
            var mostRecentFaceTimestamp = mostRecentJobResult.Faces
                .Max(face => face.TimestampStart);

            return mostRecentFaceTimestamp;
        }

        public async Task<List<FaceDetectionResult>> GetFilteredFaces(FaceDetectionFilter faceDetectionFilter)
        {
            // Step 1: Build the filter for JobResults
            var jobFilters = new List<FilterDefinition<JobResult>>();

            // Filter by ChannelIds
            if (faceDetectionFilter.ChannelIds.Count > 0)
            {
                jobFilters.Add(Builders<JobResult>.Filter.In(jr => jr.ChannelId, faceDetectionFilter.ChannelIds));
            }

            // Filter by TimestampStart and TimestampEnd
            jobFilters.Add(Builders<JobResult>.Filter.Lte(jr => jr.Start, faceDetectionFilter.TimestampEnd));
            jobFilters.Add(Builders<JobResult>.Filter.Gte(jr => jr.End, faceDetectionFilter.TimestampStart));

            // Combine all filters using AND
            var combinedFilter = jobFilters.Any()
                ? Builders<JobResult>.Filter.And(jobFilters)
                : FilterDefinition<JobResult>.Empty;

            // Step 2: Query the database for matching JobResults
            var jobResults = await _aiJobResultCollection
                .Find(combinedFilter)
                .SortBy(jr => jr.Start) // Sort chronologically by Start
                .Limit(_mongoItemsLimit)
                .ToListAsync();

            // Step 3: Group faces by channel ID
            var facesByChannel = jobResults
                .GroupBy(jr => jr.ChannelId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(jr => jr.Faces ?? new List<BoundingBoxObject>())
                        .Where(face =>
                            face.TimestampStart >= faceDetectionFilter.TimestampStart &&
                            face.TimestampEnd <= faceDetectionFilter.TimestampEnd)
                        .OrderBy(face => face.TimestampStart)
                        .ToList()
                );

            // Step 4: Create FaceDetectionResult for each channel ID in the filter
            var faceDetectionResults = faceDetectionFilter.ChannelIds
                .Select(channelId => new FaceDetectionResult
                {
                    ChannelId = channelId,
                    TimestampStart = faceDetectionFilter.TimestampStart,
                    TimestampEnd = faceDetectionFilter.TimestampEnd,
                    Faces = facesByChannel.ContainsKey(channelId) ? facesByChannel[channelId] : new List<BoundingBoxObject>()
                })
                .ToList();

            return faceDetectionResults;
        }
        public async Task<List<FaceDetectionResult>> GetFilteredObjects(FaceDetectionFilter objectDetectionFilter)
        {
            // Step 1: Build the filter for JobResults
            var jobFilters = new List<FilterDefinition<JobResult>>();

            // Filter by ChannelIds
            if (objectDetectionFilter.ChannelIds.Count > 0)
            {
                jobFilters.Add(Builders<JobResult>.Filter.In(jr => jr.ChannelId, objectDetectionFilter.ChannelIds));
            }

            // Filter by TimestampStart and TimestampEnd
            jobFilters.Add(Builders<JobResult>.Filter.Lte(jr => jr.Start, objectDetectionFilter.TimestampEnd));
            jobFilters.Add(Builders<JobResult>.Filter.Gte(jr => jr.End, objectDetectionFilter.TimestampStart));

            // Combine all filters using AND
            var combinedFilter = jobFilters.Any()
                ? Builders<JobResult>.Filter.And(jobFilters)
                : FilterDefinition<JobResult>.Empty;

            // Step 2: Query the database for matching JobResults
            var jobResults = await _aiJobResultCollection
                .Find(combinedFilter)
                .SortBy(jr => jr.Start) // Sort chronologically by Start
                .Limit(_mongoItemsLimit)
                .ToListAsync();

            // Step 3: Group faces by channel ID
            var objectsByChannel = jobResults
                .GroupBy(jr => jr.ChannelId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(jr => jr.Objects ?? new List<BoundingBoxObject>())
                        .Where(face =>
                            face.TimestampStart >= objectDetectionFilter.TimestampStart &&
                            face.TimestampEnd <= objectDetectionFilter.TimestampEnd)
                        .OrderBy(face => face.TimestampStart)
                        .ToList()
                );

            // Step 4: Create FaceDetectionResult for each channel ID in the filter
            var faceDetectionResults = objectDetectionFilter.ChannelIds
                .Select(channelId => new FaceDetectionResult
                {
                    ChannelId = channelId,
                    TimestampStart = objectDetectionFilter.TimestampStart,
                    TimestampEnd = objectDetectionFilter.TimestampEnd,
                    Faces = objectsByChannel.ContainsKey(channelId) ? objectsByChannel[channelId] : new List<BoundingBoxObject>()
                })
                .ToList();

            return faceDetectionResults;
        }
        public async Task<List<JobResult>> GetJobResultsByIdsAsync(IEnumerable<string> mongoIds)
        {
            // Validate input
            if (mongoIds == null || !mongoIds.Any())
            {
                return new List<JobResult>(); // Return an empty list if no IDs are provided
            }

            // Fetch job results from the MongoDB collection
            var jobResults = await _aiJobResultCollection.Find(job => mongoIds.Contains(job.Id)).ToListAsync();
            return jobResults;
        }
    }
}
