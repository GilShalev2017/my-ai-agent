using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ActusAgentService.Models.ActIntelligence
{
    public class JobRequestFilter
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Start { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? End { get; set; }
        public int[]? ChannelIds { get; set; }
        public int? SortDirection { get; set; }
    }

    public class BoundingBox
    {
        public float Top { get; set; } = 0f;
        public float Left { get; set; } = 0f;
        public float Right { get; set; } = 0f;
        public float Bottom { get; set; } = 0f;
    }
    public class BoundingBoxObject
    {

        public BoundingBox NormalizedBoundingBox { get; set; } = new();
        public string? FaceId { get; set; }
        public DateTime TimestampStart { get; set; } = DateTime.MinValue;
        public DateTime TimestampEnd { get; set; } = DateTime.MinValue;

        public string Description { get; set; } = "";
    }
    public class FaceDetectionResult
    {
        public List<BoundingBoxObject> Faces { get; set; } = new();
        public int ChannelId { get; set; }
        public DateTime TimestampStart { get; set; } = DateTime.MinValue;
        public DateTime TimestampEnd { get; set; } = DateTime.MinValue;
    }

    public class FaceDetectionFilter
    {
        public List<int> ChannelIds { get; set; } = new List<int>();
        public DateTime TimestampStart { get; set; } = DateTime.MinValue;
        public DateTime TimestampEnd { get; set; } = DateTime.MinValue;
    }

    public class FaceDetectionResultDTO
    {
        public List<FaceDetectionResult> Results { set; get; } = new();
    }

    public class JobResultFilter
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? Start { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? End { get; set; }
        public int[]? ChannelIds { get; set; }
        public string? Operation { get; set; }
        public string[]? Keywords { get; set; }
        public string? AiJobRequestId { get; set; }
        public int? SortDirection { get; set; }
    }

    public class Transcript
    {
        public string Text { get; set; } = null!;
        public int StartInSeconds { get; set; }
        public int EndInSeconds { get; set; }
    }

    public class TranscriptEx : Transcript
    {
        public TranscriptEx()
            : base()
        {
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MinValue;
        }
        public TranscriptEx(string text, int startInSeconds, int endInSeconds, DateTime startTime, DateTime endTime, string keyword)
            : base()
        {
            Text = text;
            StartInSeconds = startInSeconds;
            EndInSeconds = endInSeconds;
            StartTime = startTime;
            EndTime = endTime;
            //Keyword = keyword;
        }
        public TranscriptEx(Transcript transcript, DateTime startTime, DateTime endTime, string keyword)
          : base()
        {
            if (transcript == null)
                throw new ArgumentNullException(nameof(transcript));

            Text = transcript.Text;
            StartInSeconds = transcript.StartInSeconds;
            EndInSeconds = transcript.EndInSeconds;
            StartTime = startTime;
            EndTime = endTime;
            //Keyword = keyword;
        }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime StartTime { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime EndTime { get; set; }
        public string? Keyword { get; set; }
    }

    public enum ProviderType
    {
        None = 0,
        Azure,
        OpenAI,
        Whisper,
        Google,
        Neurotech,
        Speechmatix,
        GoogleVideo
    }

    public class JobResult
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string AiJobRequestId { get; set; } = null!;
        public int ChannelId { get; set; } = 0;
        public string ChannelDisplayName { get; set; } = "";
        public string Status { get; set; } = "";
        public string Operation { get; set; } = "";
        public List<TranscriptEx>? Content { get; set; }
        public List<BoundingBoxObject>? Objects { get; set; } = new();
        public List<BoundingBoxObject>? Faces { get; set; } = new();
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime End { get; set; }
        public string? FilePath { get; set; } //.mp4 or .mp3 or none
        public string? AudioLanguage { get; set; }
        public string? TranslationLanguage { get; set; }
        public ProviderType? ProviderType { get; set; }
        public string? TranscriptionJobResultId { get; set; }

        public float[]? Embedding { get; set; }
    }
}
