using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using System.Text.Json;

namespace BOEmbeddingService.Services;

public class CommonService : ICommonService
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ILoggerService _loggerService;

    public CommonService(
        IMongoDbService mongoDbService,
        ILoggerService loggerService)
    {
        _mongoDbService = mongoDbService;
        _loggerService = loggerService;
    }

    public async Task<string[]> GetFiles(string path)
    {
        List<string> files = new List<string>();
        try
        {
            foreach (string f in Directory.GetFiles(path))
            {
                files.Add(f);
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                if (!d.Contains("bin") && !d.Contains("obj"))
                {
                    var subDirFiles = await GetFiles(d);
                    files.AddRange(subDirFiles);
                }
            }


            return files.ToArray();
        }
        catch (System.Exception ex)
        {
            _loggerService.Logger.Error($"GetFiles | File: {path} | Message: {ex.Message} | Stack Trace: {ex.StackTrace}");
            throw; // Re-throw the exception to be handled by the caller
        }
    }

    public async Task WriteToFileAndDB(WriteToFileModel model)
    {
        string fileContent = string.Empty;

        if (model.IsForEmbeddings)
        {
            fileContent = $"""
							#############################################################################{Environment.NewLine}
							Model:{model.ModelName}
							Call Start Time: {model.StartTime}
							Call End Time: {model.StartTime}
							Time Taken (in seconds): {model.TimeTaken}
							Response:{Environment.NewLine}{model.Response}
							{Environment.NewLine}
							#############################################################################
							""";
        }
        else
        {
            fileContent = $"""
							#############################################################################{Environment.NewLine}
							Model:{model.ModelName}
							Call Start Time: {model.StartTime}
							Call End Time: {model.StartTime}
							Time Taken (in seconds): {model.TimeTaken}
							Total Tokens:{model.TotalTokenCount}, Input Tokens: {model.InputTokenCount}, Output Tokens: {model.OutputTokenCount}
							Prompts:
							System Prompt:{Environment.NewLine}{model.Prompts.SystemPrompt}{Environment.NewLine}
							User Prompt:{Environment.NewLine}{model.Prompts.UserPrompt} {Environment.NewLine},
							Chat Completion Options:
							{JsonSerializer.Serialize(model.ChatCompletionOptions)}
							Response:{Environment.NewLine}{model.Response}
							{Environment.NewLine}
							#############################################################################
							""";
        }
        File.AppendAllText(model.FilePath + ".txt", fileContent);
        await _mongoDbService.InsertDocumentAsync(model);
    }
}
