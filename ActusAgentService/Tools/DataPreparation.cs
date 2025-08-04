using ActusAgentService.Models;
using ActusAgentService.Services;
using System.Text.Json;

namespace ActusAgentService.Tools
{
    //public class DataPreparation
    //{
    //    private readonly EmbeddingProvider _embeddingProvider;

    //    public DataPreparation(EmbeddingProvider embeddingProvider)
    //    {
    //        _embeddingProvider = embeddingProvider;
    //    }

    //    public async Task EmbedAndSaveAsync(List<Transcript> transcripts, string outputPath)
    //    {
    //        foreach (var t in transcripts)
    //        {
    //            t.Embedding = await _embeddingProvider.EmbedTextAsync(t.Text);
    //        }

    //        string json = JsonSerializer.Serialize(transcripts, new JsonSerializerOptions { WriteIndented = true });
    //        File.WriteAllText(outputPath, json);
    //    }
    //}

}
