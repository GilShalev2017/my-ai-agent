using System.Text.Json.Serialization;

namespace ActusAgentService.Models
{
    public class QueryIntentContext
    {
        public string OriginalQuery { get; set; }

        public List<string> Intents { get; set; }

        public List<Entity> Entities { get; set; }

        public List<DateEntity> Dates { get; set; }

        public List<SourceEntity> Sources { get; set; }
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
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
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
  
    //public class QueryIntentContext
    //{
    //    public string OriginalQuery { get; set; } = string.Empty;
    //    public List<string> Intents { get; set; } = new();
    //    public List<string> Entities { get; set; } = new();
    //    public List<string> Dates { get; set; } = new();  // Could be normalized later
    //    public List<string> Sources { get; set; } = new(); // e.g., "alerts", "transcripts"
    //}

    public class QueryPlan
    {
        public string UserQuery { get; set; } = string.Empty;
        public string AdditionalInstructions { get; set; } = string.Empty;
        public string PlanId => $"{string.Join("_", Intents ?? new())}_{string.Join("_", Entities ?? new())}";
        public List<string> Intents { get; set; } = [];
        public List<string> Sources { get; set; } = [];
        public List<string> Entities { get; set; } = [];
    }

    public class AgentResponse
    {
        public string Result { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string AgentType { get; set; } = string.Empty;
    }
}
