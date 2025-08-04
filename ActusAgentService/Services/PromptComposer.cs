using System.Text;

namespace ActusAgentService.Services
{
    public class PromptComposerContext
    {
        public string UserQuery { get; set; }
        public List<string> Intents { get; set; }
        public List<string> Entities { get; set; }
        public List<string> Dates { get; set; }
        public List<string> Sources { get; set; }

        public List<string> Alerts { get; set; } = new();
        public List<string> TranscriptLines { get; set; } = new();
    }

    public class PromptComposer
    {
        public string Compose(PromptComposerContext ctx)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"User query: \"{ctx.UserQuery}\"");
            sb.AppendLine();

            if (ctx.Sources.Contains("alerts") && ctx.Alerts.Any())
            {
                sb.AppendLine("Relevant Alerts:");
                foreach (var alert in ctx.Alerts)
                    sb.AppendLine($"- {alert}");
                sb.AppendLine();
            }

            if (ctx.Sources.Contains("transcripts") && ctx.TranscriptLines.Any())
            {
                sb.AppendLine("Relevant Transcript Segments:");
                foreach (var line in ctx.TranscriptLines.Take(10)) // Optional: limit number
                    sb.AppendLine($"- {line}");
                sb.AppendLine();
            }

            sb.AppendLine("Instructions:");

            if (ctx.Intents.Contains("summarization"))
                sb.AppendLine("- Provide a concise summary of the above content.");

            if (ctx.Intents.Contains("emotion_analysis"))
                sb.AppendLine("- Identify and explain any emotional tone or sentiment.");

            if (ctx.Intents.Contains("alert_filter"))
                sb.AppendLine("- Filter only the alerts matching the topic(s): " + string.Join(", ", ctx.Entities));

            if (ctx.Intents.Contains("keyword_search"))
                sb.AppendLine("- Focus on content related to: " + string.Join(", ", ctx.Entities));

            if (ctx.Dates.Any())
                sb.AppendLine("- Limit response to content from: " + string.Join(", ", ctx.Dates));

            sb.AppendLine("- Respond clearly and in natural language.");

            return sb.ToString();
        }
    }

}
