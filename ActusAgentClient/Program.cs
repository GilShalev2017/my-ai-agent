using System.Net.Http.Json;
using System.Text;

// Define a base address and client with a timeout.
// These are not changed in the loop, so they should be set once.
var client = new HttpClient();
client.Timeout = TimeSpan.FromMinutes(10);
client.BaseAddress = new Uri("http://localhost:5270"); // adjust port

// The program will now run in a loop, continuously asking for input.
Console.WriteLine("Actus Agent Console: Type 'exit' or 'quit' to close.");

var conversationLog = new List<(string Question, string Answer)>();

while (true)
{
    Console.WriteLine("\nAsk your Actus Agent:");

    Console.ForegroundColor = ConsoleColor.Blue;

    var query = Console.ReadLine();

    Console.ResetColor();

    // Check for exit commands to break the loop.
    if (string.Equals(query, "exit", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(query, "quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // Check if the query is a valid string.
    if (!string.IsNullOrWhiteSpace(query))
    {
        try
        {
            // Post the query to the agent API.
            var response = await client.PostAsJsonAsync("/api/query/ask", query);
  
            response.EnsureSuccessStatusCode(); // Throws an exception for non-2xx status codes.

            // Read the response and print it to the console.
            string answer = await response.Content.ReadAsStringAsync();

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("\nAI Answer:\n" + answer);

            Console.ResetColor();
            
            conversationLog.Add((query, answer));
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"\nError calling API: {e.Message}");
        }
    }
    else
    {
        Console.WriteLine("Please enter a valid query.");
    }
}

// Save to HTML
string filePath = Path.Combine(Environment.CurrentDirectory, "ConversationLog.html");
var sb = new StringBuilder();
sb.AppendLine("<html><head><meta charset='UTF-8'><title>Conversation Log</title></head><body>");
foreach (var entry in conversationLog)
{
    sb.AppendLine($"<p style='color:blue;'><strong>You:</strong> {System.Net.WebUtility.HtmlEncode(entry.Question)}</p>");
    sb.AppendLine($"<p style='color:green;'><strong>AI:</strong> {System.Net.WebUtility.HtmlEncode(entry.Answer)}</p>");
    sb.AppendLine("<hr>");
}
sb.AppendLine("</body></html>");
File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

Console.WriteLine($"Conversation saved to {filePath}");

Console.WriteLine("Program has exited. Press any key to close the window...");
Console.ReadLine();
