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
        private readonly ICommonService _commonService;
        private readonly IAppSettings _appSettings;
        private string _openAiChatModelName;
        private string _openAiEmbeddingModelName;
        private int _retryCount = 2;
        private TimeSpan _networkTimeout = TimeSpan.FromMinutes(10);
        private AzureOpenAIClient _openAIClient;
        public OpenAIService(IAppSettings appSettings, ILoggerService loggerService, ICommonService commonService)
        {
            _appSettings = appSettings;
            _loggerService = loggerService;
            _commonService = commonService;
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

        public async Task<string[]> GenerateEmbeddingsAsync(string ServiceName, IEnumerable<string> texts)
        {
            try
            {
                DateTime StartTime = DateTime.Now;
                DateTime endTime = DateTime.Now;

                var embeddingClient = _openAIClient.GetEmbeddingClient(_openAiEmbeddingModelName);
                var response = await embeddingClient.GenerateEmbeddingsAsync(texts);

                endTime = DateTime.Now;

                string questionEmbeddingsFilePath = Path.Combine(_appSettings.targetDir, "PromptRequestResponse", "QuestionEmbeddings");
                if (!Path.Exists(questionEmbeddingsFilePath))
                {
                    Directory.CreateDirectory(questionEmbeddingsFilePath);
                }


                WriteToFileModel writeQuestionsEmbeddingsToFile = new WriteToFileModel
                {
                    ModelName = _openAiEmbeddingModelName,
                    IsForEmbeddings = true,
                    FilePath = Path.Combine(questionEmbeddingsFilePath, $"{ServiceName}_{DateTime.Now.ToString("yyyyMMdd_H_mm_ss")}"),
                    StartTime = StartTime,
                    EndTime = endTime,
                    TimeTaken = (endTime - StartTime).TotalSeconds,
                    texts = texts,
                    Response = Newtonsoft.Json.JsonConvert.SerializeObject(response.Value)
                };
                await _commonService.WriteToFileAndDB(writeQuestionsEmbeddingsToFile);


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
