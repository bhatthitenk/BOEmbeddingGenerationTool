using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BOEmbeddingService.Services
{
	public class GenerateInterfaceSummaryService : IGenerateInterfaceSummaryService
	{
        private readonly IAppSettings _appSettings;
		private readonly ILoggerService _loggerService;
		private readonly IOpenAIService _openAIService;
		private readonly List<string> files = new List<string>();
        private readonly Uri gitRepo;
        private readonly string targetDir;


        private readonly ICommonService _commonService;
        private readonly IGenerateServiceDescription _generateServiceDescription;

		public GenerateInterfaceSummaryService(ICommonService commonService, IAppSettings appSettings,
			ILoggerService loggerService,IOpenAIService openAIService, IGenerateServiceDescription generateServiceDescription)
		{
            _appSettings = appSettings;
            _commonService = commonService;
			_loggerService = loggerService;
			_generateServiceDescription = generateServiceDescription;
			_openAIService = openAIService;

            // Assign Values From AppConfig
            gitRepo = new Uri(_appSettings.gitRepo);
            targetDir = Path.GetDirectoryName(_appSettings.targetDir);
        }

		public async Task GenerateInterfaceSummary()
		{
			try
			{
				var contractDefinitionTargetDir = Path.Combine(_appSettings.targetDir, "ContractSummaries");

				var items = await _commonService.GetFiles(_appSettings.BOObjectsLocation);
				var contractFiles = await _commonService.GetFiles(_appSettings.BOContractsLocation);

				// skip root folder
				foreach (var boRoot in items/*.Skip(1)*/) //.Where(x => x.Path.EndsWith("APInvoice")))
				{
					FileInfo fi = new FileInfo(boRoot);

					var boName = fi.Directory.Name;//Path.GetFileName(boRoot/*boRoot.Path*/);
					var serviceName = $"ERP.BO.{boName}Svc";
					var destinationFile = Path.Combine(_appSettings.targetDir, "BusinessObjectDescription", _openAIService.Model.DeploymentName, serviceName + ".json");
					Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

					if (Path.Exists(destinationFile))
						// skip if file already exists
						continue;

					var aiContextFiles = new List<CodeFile>();

					var contractInterfaceFile = contractFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == boName + "Contract"/* && !x.IsFolder*/);
					if (contractInterfaceFile == null)
					{
						continue;
					}
					//var contractContentStream = await File.ReadAllTextAsync(contractInterfaceFile); /*gitClient.GetItemTextAsync("Epicor-PD", "current-kinetic", contractInterfaceFile.Path, (string)null);*/
					StreamReader reader = new(contractInterfaceFile);
					var content = await reader.ReadToEndAsync();

					/*
                    // we place this file at position 0 to ensure it is the last one removed
                    aiContextFiles.Insert(0, new CodeFile { Content = content, Filename = Path.GetFileName(contractInterfaceFile.Path) });
                    await contentStream.DisposeAsync();
                    */


					// Generate contract summary
					FileInfo contractFile = new FileInfo(contractInterfaceFile);
					var contractSummaryFile = Path.Combine(contractDefinitionTargetDir, boName, Path.ChangeExtension(contractFile.Name, ".contract.json"));
					Directory.CreateDirectory(Path.GetDirectoryName(contractSummaryFile));
					Dictionary<string, string> contractSummary = new();

                    Console.WriteLine($"{DateTime.Now}: Interface Summary Starts: {contractSummaryFile}");
                    if (File.Exists(contractSummaryFile))
					{
						contractSummary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(contractSummaryFile));
					}
					else
					{
						string summaryFileName = string.Join(boName, Path.GetFileNameWithoutExtension(contractFile.Name));

                        contractSummary = await GenerateInterfaceImplementationSummary(summaryFileName, content, aiContextFiles.ToDictionary(x => x.Filename, x => x.Content), boName, _openAIService.Model);
						await File.WriteAllTextAsync(contractSummaryFile, System.Text.Json.JsonSerializer.Serialize(contractSummary, new JsonSerializerOptions { WriteIndented = true }));
					}
                    Console.WriteLine($"{DateTime.Now}: Interface Summary Ends: {contractSummaryFile}");

                    Console.WriteLine($"{DateTime.Now}: Service Description Starts: {contractSummaryFile}");
                    // generate description with openAI
                    var description = await _generateServiceDescription.GenerateServiceDescriptionAsync(serviceName, contractSummary, aiContextFiles, _openAIService.Model);
					if (description == null)
					{
						// BO failed to process :(
						//"Failed to process".Dump(boName);
						await File.WriteAllTextAsync(destinationFile + ".bad", "");
						continue;
					}

					//description.DumpTell();

					await File.WriteAllTextAsync(destinationFile, System.Text.Json.JsonSerializer.Serialize(description, options: new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"{DateTime.Now}: Service Description Ends: {contractSummaryFile}");
                    //break; // stop iterating during testing
                }
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private async Task<Dictionary<string, string>> GenerateInterfaceImplementationSummary(string summaryFileName, string interfaceFileContents, Dictionary<string, string> implementationFiles, string boName, AIModelDefinition model)
		{
			string systemPrompt = $$"""
		You are C# code interpreter for Epicor Kinetic ERP system. When user supplies you with interface file and summary of method implementations,
		you analyse these and return information about what each individual interface method does.
		
		{{Constants.KineticBusinessObjectImplementationDetails}}
		
		User will supply you will JSON template for response. Fill in the blank summary fields and return full JSON with method summaries.
		Your response should contain the following fields:
		* declaration - method declaration as supplied by user and matching interface member declaration
		* summary - summary of what this method does that you generate
		
		When a given interface method has additional implementation methods (like Before/After and table specific Before/After methods as described above, etc), take these into account when describing what a given method does.
		For example, when describing Update() method that has TableXBeforeCreate that validates that link to TableY in field TableX.FieldA has matchign record, this should be included in summary for Update().
		
		Only use the following code for formulate your response and do not guess beyond information supplied in these instructions and context:
		
		################ Interface File #################
		{{interfaceFileContents}}
		############### END Interface File ##############
		
		{{string.Join("\r\n", implementationFiles.Select(txt => $"############ {txt.Key} ##########\r\n{txt.Value}\r\n############ END {txt.Key} ###########\r\n"))}}
		""";
			
			var interfaceSyntax = CSharpSyntaxTree.ParseText(interfaceFileContents);
			var methods = interfaceSyntax.GetCompilationUnitRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().SelectMany(i => i.DescendantNodes().OfType<MethodDeclarationSyntax>()
				.Where(decl => decl.Body is null).Select(x => x.WithBody(null).WithLeadingTrivia(null).WithTrailingTrivia(null).WithAdditionalAnnotations().WithAttributeLists(new SyntaxList<AttributeListSyntax>()).GetText()))
				.ToList();

			// group methods into batches of 50
			var response = new List<KeyValuePair<string, string>>();
			int batchId = 0;
			foreach (var methodBatch in methods.Batch(50))
			{
				batchId++;
				var userMessageData = new
				{
					methods = methodBatch.Select(m =>
						new
						{
							declaration = m.ToString().Trim().Trim(';'),
							summary = string.Empty
						})
				};

				var userPrompt = System.Text.Json.JsonSerializer.Serialize(userMessageData);

                var completion = await _openAIService.CompleteChatAsync(
					new ChatMessage[] 
					{
						ChatMessage.CreateSystemMessage(systemPrompt),
				        ChatMessage.CreateUserMessage(),
					},
					new ChatCompletionOptions
					{
						ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
						Temperature = 0.0f,
                        //MaxTokens = 16_000
                        MaxOutputTokenCount = 16_000,
                    }
				);

                string interfaceSummaryPath = Path.Combine(_appSettings.targetDir, "PromptRequestResponse", "InterfaceImplementationSummary");
                if (!Path.Exists(interfaceSummaryPath))
                {
                    Directory.CreateDirectory(interfaceSummaryPath);
                }
                WriteToFileModel writeToFileModel = new WriteToFileModel
                {
                    ModelName = model.DeploymentName,
                    InputTokenCount = completion.Value.Usage.InputTokenCount,
                    OutputTokenCount = completion.Value.Usage.OutputTokenCount,
                    TotalTokenCount = completion.Value.Usage.TotalTokenCount,
                    FilePath = Path.Combine(interfaceSummaryPath, $"{summaryFileName}_{DateTime.Now.ToString("yyyyMMdd_H_mm_ss")}"),
                    Prompts = new Prompts { SystemPrompt = systemPrompt, UserPrompt = userPrompt },
                    Response = string.Join("\r\n", completion.Value.Content.Select(c => $"### {c.Text} ###"))
                };
                await _commonService.WriteToFileAndDB(writeToFileModel);

				var responseJson = JsonNode.Parse(completion.Value.Content.Last().Text);
				response.AddRange(responseJson["methods"].AsArray()
					.Select(node => new { declaration = node["declaration"]?.AsValue().GetValue<string>(), summary = node["summary"]?.AsValue().GetValue<string>() })
					.ToDictionary(x => x.declaration, x => x.summary));
			}

			return response.GroupBy(x => x.Key).ToDictionary(x => x.Key, y => y.Last().Value);
		}
	}
}
