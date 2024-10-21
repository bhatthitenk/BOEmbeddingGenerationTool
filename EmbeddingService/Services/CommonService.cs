using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;

namespace BOEmbeddingService.Services
{
    public class CommonService : ICommonService
	{
		private readonly IMongoDbService _mongoDbService;
		private readonly ILoggerService _loggerService;

		public CommonService(IMongoDbService mongoDbService, ILoggerService loggerService)
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
                _loggerService._logger.Error($"GetFiles | File: {path} | Message: {ex.Message} | Stack Trace: {ex.StackTrace}");
                throw; // Re-throw the exception to be handled by the caller
            }
        }

		public async Task WriteToFileAndDB(WriteToFileModel model)
        {
			var fileContent = $$$"""
								#############################################################################
								Model:{{{model.ModelName}}} {{{Environment.NewLine}}}
								Total Tokens:{{{model.TotalTokenCount}}}, Input Tokens: {{{model.InputTokenCount}}}, Output Tokens: {{{model.OutputTokenCount}}} {{{Environment.NewLine}}}
								Prompts:{{{Environment.NewLine}}}
								System Prompt: {{{model.Prompts.SystemPrompt}}}{{{Environment.NewLine}}}{{{Environment.NewLine}}}
								User Prompt: {{{model.Prompts.UserPrompt}}}
								Response: {{{model.Response}}}
								{{{Environment.NewLine}}}
								#############################################################################
								""";
            File.AppendAllText(model.FilePath + ".txt", fileContent);
			await _mongoDbService.InsertDocumentAsync(model);
		}
	}
}
