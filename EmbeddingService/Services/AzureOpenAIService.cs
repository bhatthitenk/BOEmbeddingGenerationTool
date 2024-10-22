using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace BOEmbeddingService.Services;
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILoggerService _loggerService;
    private readonly AIModelDefinition _modelDefinition;
    private readonly ICommonService _commonService;
    private readonly AppSettings _appSettings;
    private string _azureOpenAIChatModelName;
    private string _azureOpenAIEmbeddingModelName;
    private int _retryCount = 2;
    private TimeSpan _networkTimeout = TimeSpan.FromMinutes(10);
    private AzureOpenAIClient _azureOpenAIClient;
    public AIModelDefinition Model => _modelDefinition;

    public AzureOpenAIService(
        AppSettings appSettings,
        ILoggerService loggerService,
        ICommonService commonService)
    {
        _appSettings = appSettings;
        _loggerService = loggerService;
        _commonService = commonService;
        //Setting up values from AppSetting
        _azureOpenAIChatModelName = _appSettings.AzureOpenAIChatModelName;
        _azureOpenAIEmbeddingModelName = _appSettings.AzureOpenAIEmbeddingModelName;
        _modelDefinition = new AIModelDefinition(_azureOpenAIChatModelName, 0.00275m / 1000, 0.011m / 1000); //TBD - need to think of getting cost from configuration. 
        _azureOpenAIClient = new AzureOpenAIClient(
            new Uri(_appSettings.AzureOpenAIEndpoint),
            new ApiKeyCredential(_appSettings.AzureOpenAIKey),
            new AzureOpenAIClientOptions
            {
                RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(_retryCount),
                NetworkTimeout = _networkTimeout
            }
        );
    }

    public async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
    {
        try
        {
            var chatClient = _azureOpenAIClient.GetChatClient(_azureOpenAIChatModelName);
            return await chatClient.CompleteChatAsync(messages, options);
        }
        catch (Exception ex)
        {
            _loggerService.Logger.Error($"CompleteChatAsync: {ex.Message} | Stack Trace: {ex.StackTrace}");
            throw; // Re-throw the exception to be handled by the caller
        }
    }

    public async Task<string[]> GenerateEmbeddingsAsync(string serviceName, IEnumerable<string> texts)
    {
        try
        {
            DateTime StartTime = DateTime.Now;
            DateTime endTime = DateTime.Now;

            var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_azureOpenAIEmbeddingModelName);
            var response = await embeddingClient.GenerateEmbeddingsAsync(texts);

            endTime = DateTime.Now;

            string questionEmbeddingsFilePath = Path.Combine(_appSettings.TargetDir, "PromptRequestResponse", "QuestionEmbeddings");
            if (!Path.Exists(questionEmbeddingsFilePath))
            {
                Directory.CreateDirectory(questionEmbeddingsFilePath);
            }


            WriteToFileModel writeQuestionsEmbeddingsToFile = new WriteToFileModel
            {
                ModelName = _azureOpenAIEmbeddingModelName,
                IsForEmbeddings = true,
                FilePath = Path.Combine(questionEmbeddingsFilePath, $"{serviceName}_{DateTime.Now.ToString("yyyyMMdd_H_mm_ss")}"),
                StartTime = StartTime,
                EndTime = endTime,
                TimeTaken = (endTime - StartTime).TotalSeconds,
                Texts = texts,
                Response = Newtonsoft.Json.JsonConvert.SerializeObject(response.Value)
            };
            await _commonService.WriteToFileAndDB(writeQuestionsEmbeddingsToFile);


            return response.Value.Select(x => x.Index.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _loggerService.Logger.Error($"GenerateEmbeddingsAsync: {ex.Message} | Stack Trace: {ex.StackTrace}");
            throw; // Re-throw the exception to be handled by the caller
        }
    }
}
