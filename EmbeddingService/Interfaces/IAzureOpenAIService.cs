using BOEmbeddingService.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace BOEmbeddingService.Interfaces;

public interface IAzureOpenAIService
{
    AIModelDefinition Model { get; }
    Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options);
    Task<string[]> GenerateEmbeddingsAsync(string ServiceName, IEnumerable<string> texts);
}
