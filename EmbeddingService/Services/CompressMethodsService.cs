using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenAI.Chat;
using System.Text.Json;

namespace BOEmbeddingService.Services
{
	public class CompressMethodsService : ICompressMethodsService
	{
        private readonly IAppSettings _appSettings;
        private readonly ICommonService _commonService;
        private readonly IOpenAIService _openAIService;
        public CompressMethodsService(ICommonService commonService, IAppSettings appSettings, IOpenAIService openAIService)
		{
			_commonService = commonService;
			_appSettings = appSettings;	
			_openAIService = openAIService;
		}

		public async Task GetCompressMethods()
		{
			try
			{
				/********** CHANGE THIS TO SWAP MODELS! ********/
				//var model = gpt_4o_mini;
				//var model = gpt_4o;
				/***********************************************/
				//totalCostDumper.Dump("Total Cost");
				Directory.CreateDirectory(_appSettings.targetDir);
				var codeFileTargetDir = Path.Combine(_appSettings.targetDir, "CompressedCodeFiles");
				Directory.CreateDirectory(codeFileTargetDir);

				var items = await _commonService.GetFiles(_appSettings.BOObjectsLocation);

				// skip root folder
				foreach (var boRoot in items/*.Skip(1)*/) //.Where(x => x.Path.EndsWith("APInvoice")))
				{
					FileInfo fi = new FileInfo(boRoot);

					var boName = fi.Directory.Name;//Path.GetFileName(boRoot/*boRoot.Path*/);
					var aiContextFiles = new List<CodeFile>();

					//var boFiles = await gitClient.GetItemsAsync("Epicor-PD", "current-kinetic", boRoot.Path, recursionLevel: VersionControlRecursionType.OneLevel);

					// main service logic overrides
					var mainCodeFile = boRoot; /*boFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Path) == boName && !x.IsFolder);*/
					var compressedCodeFile = Path.Combine(codeFileTargetDir, boName, fi.Name./*Path.*/TrimStart('/', '\\'));
					Directory.CreateDirectory(Path.GetDirectoryName(compressedCodeFile));

                    Console.WriteLine($"{DateTime.Now}: Process Starts: {compressedCodeFile}");
                    // main code compression
                    if (File.Exists(compressedCodeFile))
					{
						aiContextFiles.Add(new CodeFile { Content = await File.ReadAllTextAsync(compressedCodeFile), Filename = Path.GetFileName(mainCodeFile/*.Path*/) });
					}
					else
					{
						try
						{
							//var mainFileContentStream = File.ReadAllText(mainCodeFile); /*gitClient.GetItemTextAsync("Epicor-PD", "current-kinetic", mainCodeFile.Path, (string)null)*/
							StreamReader mainFileReader = new(mainCodeFile);
							var mainContent = await mainFileReader.ReadToEndAsync();

                            var compressedRequestResponseFolder = Path.Combine(Path.GetDirectoryName(_appSettings.targetDir), "CompressedRequestResponse");
                            if (!Path.Exists(compressedRequestResponseFolder))
                            {
                                Directory.CreateDirectory(compressedRequestResponseFolder);
                            }

                            var compressed = await CompressCodeFileAsync(Path.Combine(Path.GetDirectoryName(_appSettings.targetDir), "CompressedRequestResponse"), fi.Name./*Path.*/TrimStart('/', '\\'), mainContent, boName, 1, _openAIService.Model);
                            //compressed.DumpTell();
                            await File.WriteAllTextAsync(compressedCodeFile, compressed);
							aiContextFiles.Add(new CodeFile { Content = compressed, Filename = Path.GetFileName(mainCodeFile) });
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.ToString());
						}
					}
                    Console.WriteLine($"{DateTime.Now}: Process Ends: {compressedCodeFile}");
                }
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

        private async Task<string> CompressCodeFileAsync(string filepath, string filename, string fullText, string className, int maxTokens, AIModelDefinition model)
		{
			var encoder = Tiktoken.ModelToEncoder.For(model.DeploymentName);
			var tokens = encoder.CountTokens(fullText);
			//tokens.Dump(className + " BEFORE");
			if (tokens > maxTokens)
			{
				// trim needed

				var documentTree = CSharpSyntaxTree.ParseText(fullText);
				var methods = documentTree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
					.Select(m => new MethodSourceInfo
					{
						Identifier = m.Identifier.ValueText,
						Source = m.Body?.GetText().ToString(),
						Span = m.FullSpan,
						Node = m,
						Tokens = encoder.CountTokens(m.ToFullString()),
					});

				// optimiseIdentifiers
				foreach (var overload in methods.GroupBy(m => m.Identifier).Where(g => g.Count() > 1).SelectMany(grp => grp.ToArray()))
				{
					// these are different overloads of the same method, so let's optimise their identifiers by adding random GUID to the end
					overload.Identifier += Guid.NewGuid().ToString("N");
				}

				var normalised = await ReduceMethods(filepath, filename, methods.ToList(), className, model);
				/*var root = documentTree.GetCompilationUnitRoot();
                root = root.ReplaceNodes(normalised.Select(x => x.Node), 
                    (a, b) => a.WithBody(SyntaxFactory.Block(SyntaxFactory.EmptyStatement()
                        .WithLeadingTrivia(SyntaxFactory.Comment(normalised.First(x => x.Node.Span == a.Span).Source ?? "{ }")))));
                */

				var replacementClass = SyntaxFactory.CompilationUnit()
					.AddMembers(SyntaxFactory.ClassDeclaration(
						SyntaxFactory.Identifier(className)
							.WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
							.WithTrailingTrivia(SyntaxFactory.Whitespace(" ")))
						.AddMembers(normalised.Select(m => m.Node.WithBody(SyntaxFactory.Block(SyntaxFactory.EmptyStatement()
							.WithLeadingTrivia(SyntaxFactory.Comment(m.Source ?? ""))))).ToArray()));

				using var sw = new StringWriter();
				replacementClass.WriteTo(sw);
				await sw.FlushAsync();
				fullText = sw.ToString();
				encoder.CountTokens(fullText);//.Dump(className + " AFTER");
			}


			return fullText;
		}

        private async Task<IList<MethodSourceInfo>> ReduceMethods(string filepath, string filename, IList<MethodSourceInfo> methods, string serviceName, AIModelDefinition model)
		{
			if(filename == null) 
				return methods;

			if(filename.Contains("Designer.cs"))
				return methods;

			// from https://stackoverflow.com/questions/32105215/how-do-you-use-linq-to-group-records-based-on-an-accumulator
			int groupTotal = 0;
			int groupMethods = 0;
			int groupId = 0;
			int maxTokensPerGroup = 80_000; // arbitrary
			int maxMethodsPerGroup = 25; // arbitrary
			var batches = methods.Where(m => (m.Node.Body?.Statements.Count ?? 0) > 1).Select(m =>
			{
				int accumulator = groupTotal + m.Tokens;
				if (accumulator > maxTokensPerGroup || groupMethods > maxMethodsPerGroup)
				{
					groupId++;
					groupMethods = 0;
					groupTotal = m.Tokens;
				}
				else
				{
					groupMethods++;
					groupTotal = accumulator;
				}
				return new { Group = groupId, Method = m };
			});
			foreach (var batch in batches.GroupBy(b => b.Group))
			{
				var compressed = await CompressMethods(filepath, filename, serviceName, serviceName + "::" + batch.Key, batch.GroupBy(x => x.Method.Identifier).ToDictionary(x => x.Key, x => x.Last().Method.Node.ToFullString()), model);

				foreach (var compressedMethod in compressed)
				{
					var match = methods.FirstOrDefault(m => m.Identifier == compressedMethod.Key);
					if (match != null)
						match.Source = "/*\r\n" + compressedMethod.Value + "\r\n*/";
				}
				//method.Source = "/*\r\n" + completion.Value.Content.Last().Text.Replace("```pseudocode", string.Empty).Replace("```", string.Empty) + "\r\n*/";
			}

			// process the ones that failed
			int unprocessedCycle = 0;
			while (methods.Where(m => m.Source is not null && !m.Source.StartsWith("/*")).ToArray() is { Length: > 0 } unprocessedMethods)
			{
				unprocessedCycle++;
				var compressed = await CompressMethods(filepath, filename, serviceName, serviceName + "::unprocessed::" + unprocessedCycle, unprocessedMethods.GroupBy(x => x.Identifier).ToDictionary(x => x.Key, x => x.Last().Node.ToFullString()), model);
				foreach (var compressedMethod in compressed)
				{
					var match = methods.FirstOrDefault(m => m.Identifier == compressedMethod.Key);
					if (match != null)
						match.Source = "/*\r\n" + compressedMethod.Value + "\r\n*/";
				}

				if (unprocessedCycle > 10) // arbitrary
				{
					break;
				}
			}
			return methods;
		}

		/// <summary>
		/// Compress methods to replace them with summary of what each method does
		/// </summary>
		private async Task<Dictionary<string, string>> CompressMethods(string filepath, string filename, string serviceName, string callIdentifier, Dictionary<string, string> methods, AIModelDefinition model)
		{
			//var options = new AzureOpenAIClientOptions();
			//options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(5);
			//OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);
			try
			{
				string systemPrompt = """
				You are a code analysis assistant. When supplied with implementation of a method body from ERP system,
				you generate summary of what logic in this method does, aimed at software developer audience.
				Be concise and avoid unnecessary and meaningless statements - your goal is to summarize logic overall and functionality only, not line by line.
				Identify most important logic in the method and summarize it only. Spurious initializations and defaults are to only be included if they are relevant and key to understanding algorithms within.
				Your summary should be no longer than 1000 tokens and as short as possible. Do not duplicate any code from the method. Just provide brief and concise summary.
				Aim for as short a summary as possible. Ideally summary should just construe 3-5 sentences.
				Only summarize method body, do not copy declaration - it is only there as a reference.
				
				Do not format your response with Markdown code block. Just return summary without any extra blocks surrounding it.
				
				Your response should come as JSON object with keys being method names and values the method summary, e.g.
				{
					"method1": "Generate sample data for concatenation of parameter1 and parameter2"
				}
				
				For methods that have no implementation generate empty string as summary. Do not include original code in your response (if absolutely necessary, verbally summarize it) - it should be just a text description of what a given method does.
				Remember - summarize every method that has implementation that was provided by user.
				
				Use will supply methods in the following format:
				### <MethodName> ###
				<method implementation code>
				""";
				string userPrompt = string.Join("\r\n\r\n", methods.Select(b => $"### {b.Key} ###\r\n{b.Value}"));

				var completion = await _openAIService.CompleteChatAsync(new ChatMessage[]
					{
			ChatMessage.CreateSystemMessage(systemPrompt),
			ChatMessage.CreateUserMessage(userPrompt),
					}, new ChatCompletionOptions
					{
						Temperature = 0.0f,
						//MaxTokens = 16000,
						ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
					});

				var jsonData = JsonDocument.Parse(completion.Value.Content.Last().Text).RootElement.EnumerateObject().Select(token => new { Name = token.Name, Summary = token.Value.GetString() }).ToArray();

				string boFilePath = Path.Combine(filepath, serviceName);
				if (!Path.Exists(boFilePath))
				{
					Directory.CreateDirectory(boFilePath);
				}

				WriteToFileModel writeToFileModel = new WriteToFileModel
				{
					ModelName = model.DeploymentName,
					InputTokenCount = completion.Value.Usage.InputTokenCount,
					OutputTokenCount = completion.Value.Usage.OutputTokenCount,
					TotalTokenCount = completion.Value.Usage.TotalTokenCount,
					FilePath = Path.Combine(boFilePath, filename),
					Prompts = new Prompts { SystemPrompt = systemPrompt, UserPrompt = userPrompt },
					Response = string.Join("\r\n", completion.Value.Content.Select(c => $"### {c.Text} ###"))
				};
				await _commonService.WriteToFile(writeToFileModel);

				return jsonData.GroupBy(x => x.Name).ToDictionary(x => x.Key, y => y.Last().Summary);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return methods;
			}
		}
	}
}
