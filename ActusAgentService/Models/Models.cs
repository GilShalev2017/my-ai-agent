namespace ActusAgentService.Models
{
    public class Transcript
    {
        public string ChannelId { get; set; }
        public DateTime StartTime { get; set; }
        public string Text { get; set; }
        public string Topic { get; set; }
        public string Date { get; set; }
        public float[] Embedding { get; set; }
    }

    public class QueryIntentContext
    {
        public List<string> Intents { get; set; } = [];
        public List<string> Entities { get; set; } = [];
        public List<string> Dates { get; set; } = [];
        public List<string> Sources { get; set; } = [];
    }

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
