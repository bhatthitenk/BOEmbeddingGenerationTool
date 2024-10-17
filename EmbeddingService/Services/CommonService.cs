using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;

namespace BOEmbeddingService.Services
{
    public class CommonService : ICommonService
	{
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
			}
			catch (System.Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return files.ToArray();
		}

		public async Task WriteToFile(WriteToFileModel model)
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
		}
	}
}
