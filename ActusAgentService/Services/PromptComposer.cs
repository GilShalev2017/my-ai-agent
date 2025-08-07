using ActusAgentService.Models;
using System.Text;

namespace ActusAgentService.Services
{
    public interface IPromptComposer
    {
        (string systemMessage, string data) Compose(QueryIntentContext context, QueryPlan plan);
    }
    /// <summary>
    /// Turn context + plan into a smart LLM prompt
    /// </summary>
    public class PromptComposer: IPromptComposer
    {
        private string GenerateTaskDescription(List<string> intents, List<Entity> entities)
        {
            if (intents.Contains("Summarize", StringComparer.OrdinalIgnoreCase))
            {
                return "Please summarize the key topics and insights based on the above content.";
            }

            if (intents.Contains("DetectKeywords", StringComparer.OrdinalIgnoreCase))
            {
                return "Please identify important or recurring keywords related to the entities.";
            }

            if (intents.Contains("EmotionAnalysis", StringComparer.OrdinalIgnoreCase))
            {
                return "Analyze the emotional tone or sentiment in the above transcripts.";
            }

            if (intents.Contains("CheckAlerts", StringComparer.OrdinalIgnoreCase))
            {
                return "Analyze the transcripts and alerts for potential issues or warnings.";
            }

            // Default fallback
            return "Provide an analysis based on the above context and transcripts.";
        }

        public (string systemMessage, string data) Compose(QueryIntentContext context, QueryPlan plan)
        {
            var sbSystem = new StringBuilder();
            var sbUser = new StringBuilder();

            // System message includes user query and task
            sbSystem.AppendLine("You are an expert assistant that analyzes media data.");
            sbSystem.AppendLine();
            sbSystem.AppendLine("--- USER QUERY ---");
            sbSystem.AppendLine(context.OriginalQuery);
            //sbSystem.AppendLine();
            //sbSystem.AppendLine("--- TASK ---");
            //sbSystem.AppendLine(GenerateTaskDescription(context.Intents, context.Entities));

            // User message includes the actual transcripts and alerts
            if (plan.TranscriptLines.Any())
            {
                sbUser.AppendLine("--- TRANSCRIPTS ---");
                foreach (var line in plan.TranscriptLines)
                {
                    sbUser.AppendLine("- " + line);
                }
            }

            if (plan.Alerts?.Any() == true)
            {
                sbUser.AppendLine();
                sbUser.AppendLine("--- ALERTS ---");
                foreach (var alert in plan.Alerts)
                {
                    sbUser.AppendLine("- " + alert);
                }
            }

            return (sbSystem.ToString(), sbUser.ToString());
        }

    }
}

//public string Compose(QueryIntentContext context, QueryPlan plan)
//{
//    var sb = new StringBuilder();

//    // System message
//    sb.AppendLine("You are an expert assistant that analyzes media data.");
//    sb.AppendLine("--- CONTEXT ---");
//    sb.AppendLine($"Intents: {string.Join(", ", context.Intents)}");
//    sb.AppendLine($"Entities: {string.Join(", ", context.Entities)}");
//    sb.AppendLine($"Dates: {string.Join(", ", context.Dates)}");
//    sb.AppendLine($"Sources: {string.Join(", ", context.Sources)}");

//    // Transcripts
//    if (plan.TranscriptLines.Any())
//    {
//        sb.AppendLine("--- TRANSCRIPTS ---");
//        //foreach (var line in plan.TranscriptLines.Take(10))
//        foreach (var line in plan.TranscriptLines)
//                sb.AppendLine("- " + line);
//    }

//    // Alerts
//    //if (plan.Alerts.Any())
//    //{
//    //    sb.AppendLine("--- ALERTS ---");
//    //    foreach (var alert in plan.Alerts)
//    //        sb.AppendLine("- " + alert);
//    //}

//    // Task
//    sb.AppendLine("--- TASK ---");
//    string task = GenerateTaskDescription(context.Intents, context.Entities);
//    sb.AppendLine(task);

//    return sb.ToString();
//}
