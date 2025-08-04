using System.Text;

namespace ActusAgentService.Services
{
    public static class PromptBuilder
    {
        public static string BuildPrompt(string question, List<string> texts)
        {
            return $"User: {question}\n\nContext:\n" + string.Join("\n---\n", texts);
        }

        public static string BuildSummarizationPrompt(string userQuery, List<string> transcriptTexts)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"User question: {userQuery}");
            builder.AppendLine("Based on the following transcript snippets, generate a detailed answer:");
            foreach (var text in transcriptTexts)
            {
                builder.AppendLine("\n---\n" + text);
            }
            return builder.ToString();
        }
    }

}
