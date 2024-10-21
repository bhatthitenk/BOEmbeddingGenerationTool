using BOEmbeddingService.Models;
using OpenAI.Chat;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClientModel;
using BOEmbeddingService.Interfaces;
using Azure.AI.OpenAI;
using System.Runtime;

namespace BOEmbeddingService.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly ILoggerService _loggerService;
        private readonly AIModelDefinition _modelDefinition;
        private readonly IAppSettings _appSettings;
        private string _openAiChatModelName;
        private string _openAiEmbeddingModelName;
        private int _retryCount = 2;
        private TimeSpan _networkTimeout = TimeSpan.FromMinutes(10);
        private AzureOpenAIClient _openAIClient;
        public OpenAIService(IAppSettings appSettings, ILoggerService loggerService)
        {
            _appSettings = appSettings;
            _loggerService = loggerService;
            //Setting up values from AppSetting
            _openAiChatModelName = _appSettings.openAiChatModelName;
            _openAiEmbeddingModelName = _appSettings.openAiEmbeddingModelName;
            _modelDefinition = new AIModelDefinition(_openAiChatModelName, 0.00275m / 1000, 0.011m / 1000); //TBD - need to think of getting cost from configuration. 
            _openAIClient = new AzureOpenAIClient(
                                                    new Uri(_appSettings.openAiEndpoint),
                                                    new ApiKeyCredential(_appSettings.openAiKey),
                                                    new AzureOpenAIClientOptions
                                                    {
                                                        RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(_retryCount),
                                                        NetworkTimeout = _networkTimeout
                                                    }
            );
        }

		public AIModelDefinition Model => _modelDefinition;

        public async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
        {
            try
            {
                var chatClient = _openAIClient.GetChatClient(_openAiChatModelName);
                return await chatClient.CompleteChatAsync(messages, options);
            }
            catch (Exception ex)
            {
                _loggerService._logger.Error($"CompleteChatAsync: {ex.Message} | Stack Trace: {ex.StackTrace}");
                throw; // Re-throw the exception to be handled by the caller
            }
        }

        public async Task<string[]> GenerateEmbeddingsAsync(IEnumerable<string> texts)
        {
            try
            {
                var embeddingClient = _openAIClient.GetEmbeddingClient(_openAiEmbeddingModelName);
                var response = await embeddingClient.GenerateEmbeddingsAsync(texts);
                return response.Value.Select(x => x.Index.ToString()).ToArray();
            }
            catch (Exception ex)
            {
                _loggerService._logger.Error($"GenerateEmbeddingsAsync: {ex.Message} | Stack Trace: {ex.StackTrace}");
                throw; // Re-throw the exception to be handled by the caller
            }
        }
    }
}
