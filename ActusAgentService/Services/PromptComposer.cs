using ActusAgentService.Models;
using System.Text;

namespace ActusAgentService.Services
{
    public interface IPromptComposer
    {
        string Compose(QueryIntentContext context, QueryPlan plan);
    }

    public class PromptComposer: IPromptComposer
    {
        public string Compose(QueryIntentContext context, QueryPlan plan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert assistant that analyzes media data.");
            sb.AppendLine("--- CONTEXT ---");
            sb.AppendLine($"Intents: {string.Join(", ", context.Intents)}");
            sb.AppendLine($"Entities: {string.Join(", ", context.Entities)}");
            sb.AppendLine($"Dates: {string.Join(", ", context.Dates)}");
            sb.AppendLine($"Sources: {string.Join(", ", context.Sources)}");

            if (plan.TranscriptLines.Any())
            {
                sb.AppendLine("--- TRANSCRIPTS ---");
                foreach (var line in plan.TranscriptLines.Take(10))
                    sb.AppendLine("- " + line);
            }

            if (!string.IsNullOrWhiteSpace(plan.FinalPrompt))
            {
                sb.AppendLine("--- TASK ---");
                sb.AppendLine(plan.FinalPrompt);
            }

            return sb.ToString();
        }
    }

}