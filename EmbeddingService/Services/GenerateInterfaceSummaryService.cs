﻿using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json.Nodes;

namespace BOEmbeddingService.Services
{
    public class GenerateInterfaceSummaryService : IGenerateInterfaceSummaryService
	{

        static appSettings _appSettings = Configuration.BuildAppSettings();

        Serilog.Core.Logger _logger = LoggerService.GetInstance();
        List<string> files = new List<string>();
        Uri gitRepo = new Uri(_appSettings.gitRepo);
        string openAiEndpoint = _appSettings.openAiEndpoint;
        string openAiEmbeddingModelName = _appSettings.openAiEmbeddingModelName;
        ApiKeyCredential openAiKey = new ApiKeyCredential(_appSettings.openAiKey);
        string targetDir = Path.GetDirectoryName(_appSettings.targetDir);


        private readonly ICommonService _commonService;

		public GenerateInterfaceSummaryService(ICommonService commonService)
		{
			_commonService = commonService;
		}

		public async Task<Dictionary<string, string>> GenerateInterfaceImplementationSummary(string interfaceFileContents, Dictionary<string, string> implementationFiles, string boName, AIModelDefinition model)
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
			var options = new AzureOpenAIClientOptions();
			options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(2);
			options.NetworkTimeout = TimeSpan.FromMinutes(10);
			OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);

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

				var aiClient = openAiClient.GetChatClient(model.DeploymentName);
				var completion = await aiClient.CompleteChatAsync(new ChatMessage[] {
			ChatMessage.CreateSystemMessage(systemPrompt),
            //ChatMessage.CreateSystemMessage("Json. Am I able to connect My AI services"),
            ChatMessage.CreateUserMessage(System.Text.Json.JsonSerializer.Serialize(userMessageData)),
            //ChatMessage.CreateUserMessage(System.Text.Json.JsonSerializer.Serialize("Json. Give me some random data.")),
        },
				new ChatCompletionOptions
				{
					ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
					Temperature = 0.0f,
					//MaxTokens = 16_000
				});

				var responseJson = JsonNode.Parse(completion.Value.Content.Last().Text);
				response.AddRange(responseJson["methods"].AsArray()
					.Select(node => new { declaration = node["declaration"]?.AsValue().GetValue<string>(), summary = node["summary"]?.AsValue().GetValue<string>() })
					.ToDictionary(x => x.declaration, x => x.summary));
			}

			return response.GroupBy(x => x.Key).ToDictionary(x => x.Key, y => y.Last().Value);
		}
	}
}
