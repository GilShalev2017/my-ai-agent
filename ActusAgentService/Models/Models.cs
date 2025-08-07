using ActusAgentService.Models.ActIntelligence;
using System.Text.Json.Serialization;

namespace ActusAgentService.Models
{
    public class QueryIntentContext
    {
        public string OriginalQuery { get; set; }
        public string RawJsonResponse { get; set; }

        public List<string> Intents { get; set; }

        public List<Entity> Entities { get; set; }

        public List<DateEntity> Dates { get; set; }

        public List<SourceEntity> Sources { get; set; }
        //public List<int> ChannelIds { get; set; }
    }
    public class Entity
    {
        [JsonPropertyName("entity")]
        public string EntityName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class DateEntity
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; }

        // Optional: Still include this if "date" is used for single date types
        [JsonPropertyName("date")]
        public string Date { get; set; }
    }


    public class SourceEntity
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class Transcript
    {
        public string ChannelId { get; set; }
        public DateTime StartTime { get; set; }
        public string Text { get; set; }
        public string Topic { get; set; }
        public string Date { get; set; }
        public float[] Embedding { get; set; }
    }

    //public class QueryPlan
    //{
    //    public string UserQuery { get; set; } = string.Empty;
    //    public string AdditionalInstructions { get; set; } = string.Empty;
    //    public string PlanId => $"{string.Join("_", Intents ?? new())}_{string.Join("_", Entities ?? new())}";
    //    public List<string> Intents { get; set; } = [];
    //    public List<string> Sources { get; set; } = [];
    //    public List<string> Entities { get; set; } = [];
    //}

    public class QueryPlan
    {
        public string QueryHash { get; set; } // hash of user query to cache
        public string UserQuery { get; set; }
        public List<string> Intents { get; set; }
        public List<string> Entities { get; set; }
        public List<string> Dates { get; set; }
        public List<DateEntity> RawDates { get; set; }
        public List<string> Sources { get; set; }

        public List<string> Alerts { get; set; }
        public List<string> TranscriptLines { get; set; }

        public string FinalPrompt { get; set; }
        public string AdditionalInstructions { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsExpired => (DateTime.UtcNow - CreatedAt).TotalMinutes > 10; // optional TTL
        public JobResultFilter Filter { get; set; }
    }

    public class AgentResponse
    {
        public string Result { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string AgentType { get; set; } = string.Empty;
    }
}
