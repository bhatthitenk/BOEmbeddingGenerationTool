using OpenAI.Chat;

namespace BOEmbeddingService.Models;

public class WriteToFileModel
{
    public string ModelName { get; set; }
    public int InputTokenCount { get; set; }
    public int OutputTokenCount { get; set; }
    public int TotalTokenCount { get; set; }
    public string FilePath { get; set; }
    public Prompts Prompts { get; set; }
    public string Response { get; set; }
    public bool IsForEmbeddings { get; set; }
    public IEnumerable<string> Texts { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TimeTaken { get; set; }
    public ChatCompletionOptions ChatCompletionOptions { get; set; }
}

public class Prompts
{
    public string SystemPrompt { get; set; }
    public string UserPrompt { get; set; }
}
