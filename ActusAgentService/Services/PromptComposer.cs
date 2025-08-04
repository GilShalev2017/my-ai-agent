using System.Text;

namespace ActusAgentService.Services
{
    public class PromptComposer
    {
        //public string Compose(QueryIntentContext context, QueryPlan plan)
        //{
        //    var sb = new StringBuilder();

        //    sb.AppendLine("You are an expert assistant. Given the following user query and structured plan, perform the appropriate task.");
        //    sb.AppendLine("\nStructured Plan:");
        //    sb.AppendLine($"Intents: {string.Join(", ", context.Intents)}");
        //    sb.AppendLine($"Entities: {string.Join(", ", context.Entities)}");
        //    sb.AppendLine($"Dates: {string.Join(", ", context.Dates)}");
        //    sb.AppendLine($"Sources: {string.Join(", ", context.Sources)}");

        //    if (!string.IsNullOrEmpty(plan.AdditionalInstructions))
        //    {
        //        sb.AppendLine($"Instructions: {plan.AdditionalInstructions}");
        //    }

        //    sb.AppendLine("\nUser Query:");
        //    sb.AppendLine(plan.UserQuery);

        //    sb.AppendLine("\nResponse:");
        //    return sb.ToString();
        //}

        public string Compose(QueryIntentContext context, QueryPlan plan)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an expert assistant that analyzes media data (alerts, transcripts, logs).");
            sb.AppendLine("The user query has been parsed into structured intents, entities, and relevant inputs.");
            sb.AppendLine("Use the provided structured data below to simulate an intelligent response.");
            sb.AppendLine("Do not apologize or mention limitations. Assume access to the full data.");
            sb.AppendLine("Be specific, concise, and relevant to the user's intent.");

            sb.AppendLine("\n--- STRUCTURED PLAN ---");
            sb.AppendLine($"Intents: {string.Join(", ", context.Intents)}");
            sb.AppendLine($"Entities: {string.Join(", ", context.Entities)}");
            sb.AppendLine($"Dates: {string.Join(", ", context.Dates)}");
            sb.AppendLine($"Sources: {string.Join(", ", context.Sources)}");

            if (plan.TranscriptLines?.Any() == true)
            {
                sb.AppendLine("\n--- TRANSCRIPTS ---");
                foreach (var line in plan.TranscriptLines.Take(10)) // limit to 10 for brevity
                    sb.AppendLine($"- {line}");
            }

            if (plan.Alerts?.Any() == true)
            {
                sb.AppendLine("\n--- ALERTS ---");
                foreach (var alert in plan.Alerts.Take(5))
                    sb.AppendLine($"- {alert}");
            }

            if (!string.IsNullOrEmpty(plan.FinalPrompt))
            {
                sb.AppendLine("\n--- FINAL INSTRUCTIONS ---");
                sb.AppendLine(plan.FinalPrompt);
            }

            sb.AppendLine("\n--- USER QUERY ---");
            sb.AppendLine(plan.UserQuery);

            sb.AppendLine("\n--- RESPONSE ---");
            return sb.ToString();
        }

    }

}

//public class PromptComposerContext
//{
//    public string UserQuery { get; set; }
//    public List<string> Intents { get; set; }
//    public List<string> Entities { get; set; }
//    public List<string> Dates { get; set; }
//    public List<string> Sources { get; set; }

//    public List<string> Alerts { get; set; } = new();
//    public List<string> TranscriptLines { get; set; } = new();
//}

//public class PromptComposer
//{
//    public string Compose(PromptComposerContext ctx)
//    {
//        var sb = new StringBuilder();

//        sb.AppendLine($"User query: \"{ctx.UserQuery}\"");
//        sb.AppendLine();

//        if (ctx.Sources.Contains("alerts") && ctx.Alerts.Any())
//        {
//            sb.AppendLine("Relevant Alerts:");
//            foreach (var alert in ctx.Alerts)
//                sb.AppendLine($"- {alert}");
//            sb.AppendLine();
//        }

//        if (ctx.Sources.Contains("transcripts") && ctx.TranscriptLines.Any())
//        {
//            sb.AppendLine("Relevant Transcript Segments:");
//            foreach (var line in ctx.TranscriptLines.Take(10)) // Optional: limit number
//                sb.AppendLine($"- {line}");
//            sb.AppendLine();
//        }

//        sb.AppendLine("Instructions:");

//        if (ctx.Intents.Contains("summarization"))
//            sb.AppendLine("- Provide a concise summary of the above content.");

//        if (ctx.Intents.Contains("emotion_analysis"))
//            sb.AppendLine("- Identify and explain any emotional tone or sentiment.");

//        if (ctx.Intents.Contains("alert_filter"))
//            sb.AppendLine("- Filter only the alerts matching the topic(s): " + string.Join(", ", ctx.Entities));

//        if (ctx.Intents.Contains("keyword_search"))
//            sb.AppendLine("- Focus on content related to: " + string.Join(", ", ctx.Entities));

//        if (ctx.Dates.Any())
//            sb.AppendLine("- Limit response to content from: " + string.Join(", ", ctx.Dates));

//        sb.AppendLine("- Respond clearly and in natural language.");

//        return sb.ToString();
//    }
//}
