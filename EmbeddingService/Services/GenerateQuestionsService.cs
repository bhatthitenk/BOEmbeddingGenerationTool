﻿using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace BOEmbeddingService.Services;
public class GenerateQuestionsService : IGenerateQuestionsService
{
    private readonly AppSettings _appSettings;
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly ICommonService _commonService;
    private readonly ILoggerService _loggerService;


    public GenerateQuestionsService(
        AppSettings appSettings,
        IAzureOpenAIService azureOpenAIService,
        ICommonService commonService,
        ILoggerService loggerService)
    {
        _appSettings = appSettings;
        _loggerService = loggerService;
        _azureOpenAIService = azureOpenAIService;
        _commonService = commonService;
    }
    public async Task GenerateQuestions()
    {
        // generate questions
        foreach (var descriptionFile in Directory.GetFiles(Path.Combine(_appSettings.TargetDir, "BusinessObjectDescription", _azureOpenAIService.Model.DeploymentName), "*.json"))
        {
            try
            {
                var filenameWithoutExtension = Path.GetFileNameWithoutExtension(descriptionFile);

                var questionFile = Path.Combine(_appSettings.TargetDir, "Questions", filenameWithoutExtension + ".questions.json");
                Directory.CreateDirectory(Path.GetDirectoryName(questionFile));
                if (File.Exists(questionFile))
                    continue;

                var descriptionJson = await File.ReadAllTextAsync(descriptionFile);
                var description = JsonSerializer.Deserialize<BusinessObjectDescription>(descriptionJson);


                Console.WriteLine($"{DateTime.Now}: Generate Question Starts: {descriptionFile}");
                var questions = await GenerateQuestions(description, _azureOpenAIService.Model, filenameWithoutExtension);
                await File.WriteAllTextAsync(questionFile, JsonSerializer.Serialize(questions.Select(x => new { question = x.Item1, embedding = x.Item2 }), new JsonSerializerOptions { WriteIndented = true }));

                Console.WriteLine($"{DateTime.Now}: Generate Question Ends: {descriptionFile}");
            }
            catch (Exception ex)
            {
                _loggerService.Logger.Error($"InterfaceSummary | File: {Path.GetFileName(descriptionFile)} | Message: {ex.Message} | Stack Trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Generate questions for RAG
    /// </summary>
    public async Task<List<(string, string)>> GenerateQuestions(BusinessObjectDescription description, AIModelDefinition model, string serviceName)
    {
        var prompt = """
		    You are an advanced ERP (enteprise resource processing) assistant. When provided with a description of
		    a business object (service) in Epicor Kinetic ERP system, you use this description to generate a set of 20 questions that this service could answer.
		
		    Questions you generate can have example data marked with letter abbreviations, e.g. "Do X". Do not generate questions for modifying data, all the
		    questions you create should be returned as search type questions.
		
		    Only use user supplied description to determine questions to answer. Do NOT use any other data. In the generated questions you must not mention technical implementation details, such as asking for dataset.
		    Do not put any fictitious data into the question - instead use letter placeholders only.
		
		    Format your response as json with field "questions" containing an array of strings, ne per question generated.
		    """;

        //var options = new AzureOpenAIClientOptions();
        //options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(2);
        //options.NetworkTimeout = TimeSpan.FromMinutes(10);
        //OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);

        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions
        {
            Temperature = 0.0f,
            //MaxTokens = 8000,
            MaxOutputTokenCount = 8000,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        DateTime startTime = DateTime.Now;
        DateTime endTime = DateTime.Now;

        var completion = await _azureOpenAIService.CompleteChatAsync(
            new ChatMessage[]
            {
                ChatMessage.CreateSystemMessage(prompt),
                ChatMessage.CreateUserMessage(description.Description),
            },
            chatCompletionOptions
        );
        endTime = DateTime.Now;

        string questionsFilePath = Path.Combine(_appSettings.TargetDir, "PromptRequestResponse", "Questions");

        if (!Path.Exists(questionsFilePath))
        {
            Directory.CreateDirectory(questionsFilePath);
        }

        WriteToFileModel writeToFileModel = new WriteToFileModel
        {
            ModelName = model.DeploymentName,
            InputTokenCount = completion.Value.Usage.InputTokenCount,
            OutputTokenCount = completion.Value.Usage.OutputTokenCount,
            TotalTokenCount = completion.Value.Usage.TotalTokenCount,
            FilePath = Path.Combine(questionsFilePath, $"{serviceName}_{DateTime.Now.ToString("yyyyMMdd_H_mm_ss")}"),
            Prompts = new Prompts { SystemPrompt = prompt, UserPrompt = description.Description },
            StartTime = startTime,
            EndTime = endTime,
            TimeTaken = (endTime - startTime).TotalSeconds,
            ChatCompletionOptions = chatCompletionOptions,
            Response = JsonSerializer.Serialize(completion.Value)
        };
        await _commonService.WriteToFileAndDB(writeToFileModel);

        var questions = JsonNode.Parse(completion.Value.Content.Last().Text)["questions"].AsArray().Select(x => x.AsValue().GetValue<string>());

        var embeddings = await _azureOpenAIService.GenerateEmbeddingsAsync(serviceName, questions);
        return questions.Zip(embeddings).Select(data => (data.First, data.Second.ToString())).ToList();
    }
}
