using BOEmbeddingService.Models;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
    public interface IOpenAIService
    {
        AIModelDefinition Model { get; }
        Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options);
        Task<string[]> GenerateEmbeddingsAsync(string ServiceName, IEnumerable<string> texts);
    }
}
