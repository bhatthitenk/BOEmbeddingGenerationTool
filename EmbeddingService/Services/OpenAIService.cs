using BOEmbeddingService.Models;
using OpenAI.Chat;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClientModel;

namespace BOEmbeddingService.Services
{
    public class OpenAIService
    {
        private readonly OpenAIClient _openAiClient;
        private readonly AIModelDefinition _model;

        public OpenAIService(OpenAIClient openAiClient, AIModelDefinition model)
        {
            _openAiClient = openAiClient;
            _model = model;
        }

        public async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
        {
            var chatClient = _openAiClient.GetChatClient(_model.DeploymentName);
            return await chatClient.CompleteChatAsync(messages, options);
        }

        public async Task<string[]> GenerateEmbeddingsAsync(IEnumerable<string> texts)
        {
            var embeddingClient = _openAiClient.GetEmbeddingClient("text-embedding-3-small");
            var response = await embeddingClient.GenerateEmbeddingsAsync(texts);
            return response.Value.Select(x => x.Index.ToString()).ToArray();
        }
    }
}
