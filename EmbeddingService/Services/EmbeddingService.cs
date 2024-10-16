using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace BOEmbeddingService.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IAppSettings _appSettings;
        Serilog.Core.Logger _logger = LoggerService.GetInstance();
        
        private readonly string openAiEndpoint;
        private readonly ApiKeyCredential openAiKey;
        private readonly string targetDir;


        //AIModelDefinition gpt_4o_mini = new("gpt-4o-mini", 0.000165m / 1000, 0.00066m / 1000);
        //AIModelDefinition gpt_4o = new("gpt-4o", 0.00275m / 1000, 0.011m / 1000);

        private readonly ICommonService _commonService;
		private readonly ICompressMethodsService _compressMethodsService;
		private readonly IGenerateInterfaceSummaryService _generateInterfaceSummaryService;
		private readonly IGenerateQuestionsService _generateQuestionsService;
        private readonly IGenerateServiceDescription _generateServiceDescription;

        OpenAIService openAIService = new OpenAIServiceBuilder().Build();

		public EmbeddingService(ICommonService commonService,
			ICompressMethodsService compressMethodsService,
			IGenerateInterfaceSummaryService generateInterfaceSummaryService, 
            IGenerateQuestionsService generateQuestionsService,
            IGenerateServiceDescription generateServiceDescription, IAppSettings appSettings)
		{
            _appSettings = appSettings;
			_commonService = commonService;
            _compressMethodsService = compressMethodsService;
			_generateInterfaceSummaryService = generateInterfaceSummaryService;
			_generateQuestionsService = generateQuestionsService;
            _generateServiceDescription = generateServiceDescription;

            //Setting up values from AppSetting
            openAiEndpoint = _appSettings.openAiEndpoint;
            openAiKey = new ApiKeyCredential(_appSettings.openAiKey);
            targetDir = Path.GetDirectoryName(_appSettings.targetDir);
        }

		public async Task EmbeddedBOObjects()
        {
            try
            {

                var contractDefinitionTargetDir = Path.Combine(targetDir, "ContractSummaries");
                Directory.CreateDirectory(contractDefinitionTargetDir);

                // Commented Code
                //var token = await Util.MSAL.AcquireTokenAsync("https://login.microsoftonline.com/common", "499b84ac-1321-427f-aa17-267ca6975798/.default");
                //token.DumpTell();

                // Commented Code
                //VssConnection connection = new(gitRepo, new VssAadCredential(new VssAadToken("Bearer", token.AccessToken)));
                //connection.Dump();
                //await connection.ConnectAsync();

                // for project collection change url to end with /tfs only and not the collection
                //ProjectCollectionHttpClient projectCollectionClient = connection.GetClient<ProjectCollectionHttpClient>();

                //IEnumerable<TeamProjectCollectionReference> projectCollections = projectCollectionClient.GetProjectCollections().Result;

                //projectCollections.Dump();

                //ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

                //projectClient.GetProjects().Result.Dump();
                //var gitClient = connection.GetClient<GitHttpClient>();
                //gitClient.DumpTell();
                //var repository = await gitClient.GetRepositoryAsync("Epicor-PD", "current-kinetic");
                //repository.DumpTell();

                //var items = await gitClient.GetItemsAsync("Epicor-PD", "current-kinetic", "/Source/Server/Services/BO", recursionLevel: VersionControlRecursionType.OneLevel);

                //items.DumpTell();
				var items = await _commonService.GetFiles(_appSettings.BOObjectsLocation);
                var contractFiles = await _commonService.GetFiles(_appSettings.BOContractsLocation);

				//CompressMethodCall
				_compressMethodsService.GetCompressMethods();

                // skip root folder
                foreach (var boRoot in items/*.Skip(1)*/) //.Where(x => x.Path.EndsWith("APInvoice")))
                {
					FileInfo fi = new FileInfo(boRoot);
					var boName = fi.Directory.Name;//Path.GetFileName(boRoot/*boRoot.Path*/);
					var serviceName = $"ERP.BO.{boName}Svc";
					var aiContextFiles = new List<CodeFile>();
					var destinationFile = Path.Combine(_appSettings.targetDir, "BusinessObjectDescription", openAIService.Model.DeploymentName, serviceName + ".json");
					Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
					// contract
					//var contractFiles = await gitClient.GetItemsAsync("Epicor-PD", "current-kinetic", $"/Source/Shared/Contracts/BO/{boName}", recursionLevel: VersionControlRecursionType.OneLevel);



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
                    var contractSummaryFile = Path.ChangeExtension(Path.Combine(contractDefinitionTargetDir, contractInterfaceFile.TrimStart('/', '\\')), ".contract.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(contractSummaryFile));
                    Dictionary<string, string> contractSummary = new();
                    if (File.Exists(contractSummaryFile))
                    {
                        contractSummary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(contractSummaryFile));
                    }
                    else
                    {
                        contractSummary = await _generateInterfaceSummaryService.GenerateInterfaceImplementationSummary(content, aiContextFiles.ToDictionary(x => x.Filename, x => x.Content), boName, openAIService.Model);
                        await File.WriteAllTextAsync(contractSummaryFile, System.Text.Json.JsonSerializer.Serialize(contractSummary, new JsonSerializerOptions { WriteIndented = true }));
                    }

                    // generate description with openAI
                    var description = await _generateServiceDescription.GenerateServiceDescriptionAsync(serviceName, contractSummary, aiContextFiles, openAIService.Model);
                    if (description == null)
                    {
                        // BO failed to process :(
                        //"Failed to process".Dump(boName);
                        await File.WriteAllTextAsync(destinationFile + ".bad", "");
                        continue;
                    }

                    //description.DumpTell();

                    await File.WriteAllTextAsync(destinationFile, System.Text.Json.JsonSerializer.Serialize(description, options: new JsonSerializerOptions { WriteIndented = true }));

                    //break; // stop iterating during testing
                }

                // generate questions
                foreach (var descriptionFile in Directory.GetFiles(Path.Combine(targetDir, "BusinessObjectDescription", openAIService.Model.DeploymentName), "*.json"))
                {
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(descriptionFile);

                    var questionFile = Path.Combine(targetDir, "Questions", filenameWithoutExtension + ".questions.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(questionFile));
                    if (File.Exists(questionFile))
                        continue;

                    var descriptionJson = await File.ReadAllTextAsync(descriptionFile);
                    var description = JsonSerializer.Deserialize<BusinessObjectDescription>(descriptionJson);


                    var questions = await _generateQuestionsService.GenerateQuestions(description, openAIService.Model, filenameWithoutExtension);
                    await File.WriteAllTextAsync(questionFile, JsonSerializer.Serialize(questions.Select(x => new { question = x.Item1, embedding = x.Item2 }), new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        decimal totalCost = 0m;

    //    async Task<string> CompressCodeFileAsync(string fullText, string className, int maxTokens, AIModelDefinition model)
    //    {
    //        var encoder = Tiktoken.ModelToEncoder.For(model.DeploymentName);
    //        var tokens = encoder.CountTokens(fullText);
    //        //tokens.Dump(className + " BEFORE");
    //        if (tokens > maxTokens)
    //        {
    //            // trim needed

    //            var documentTree = CSharpSyntaxTree.ParseText(fullText);
    //            var methods = documentTree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
    //                .Select(m => new MethodSourceInfo
    //                {
    //                    Identifier = m.Identifier.ValueText,
    //                    Source = m.Body?.GetText().ToString(),
    //                    Span = m.FullSpan,
    //                    Node = m,
    //                    Tokens = encoder.CountTokens(m.ToFullString()),
    //                });

    //            // optimiseIdentifiers
    //            foreach (var overload in methods.GroupBy(m => m.Identifier).Where(g => g.Count() > 1).SelectMany(grp => grp.ToArray()))
    //            {
    //                // these are different overloads of the same method, so let's optimise their identifiers by adding random GUID to the end
    //                overload.Identifier += Guid.NewGuid().ToString("N");
    //            }

    //            var normalised = await ReduceMethods(methods.ToList(), className, model);
    //            /*var root = documentTree.GetCompilationUnitRoot();
    //            root = root.ReplaceNodes(normalised.Select(x => x.Node), 
    //                (a, b) => a.WithBody(SyntaxFactory.Block(SyntaxFactory.EmptyStatement()
    //                    .WithLeadingTrivia(SyntaxFactory.Comment(normalised.First(x => x.Node.Span == a.Span).Source ?? "{ }")))));
    //            */

    //            var replacementClass = SyntaxFactory.CompilationUnit()
    //                .AddMembers(SyntaxFactory.ClassDeclaration(
    //                    SyntaxFactory.Identifier(className)
    //                        .WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
    //                        .WithTrailingTrivia(SyntaxFactory.Whitespace(" ")))
    //                    .AddMembers(normalised.Select(m => m.Node.WithBody(SyntaxFactory.Block(SyntaxFactory.EmptyStatement()
    //                        .WithLeadingTrivia(SyntaxFactory.Comment(m.Source ?? ""))))).ToArray()));

    //            using var sw = new StringWriter();
    //            replacementClass.WriteTo(sw);
    //            await sw.FlushAsync();
    //            fullText = sw.ToString();
    //            encoder.CountTokens(fullText);//.Dump(className + " AFTER");
    //        }


    //        return fullText;
    //    }
      
    //    async Task<IList<MethodSourceInfo>> ReduceMethods(IList<MethodSourceInfo> methods, string serviceName, AIModelDefinition model)
    //    {

    //        // from https://stackoverflow.com/questions/32105215/how-do-you-use-linq-to-group-records-based-on-an-accumulator
    //        int groupTotal = 0;
    //        int groupMethods = 0;
    //        int groupId = 0;
    //        int maxTokensPerGroup = 80_000; // arbitrary
    //        int maxMethodsPerGroup = 25; // arbitrary
    //        var batches = methods.Where(m => (m.Node.Body?.Statements.Count ?? 0) > 1).Select(m =>
    //        {
    //            int accumulator = groupTotal + m.Tokens;
    //            if (accumulator > maxTokensPerGroup || groupMethods > maxMethodsPerGroup)
    //            {
    //                groupId++;
    //                groupMethods = 0;
    //                groupTotal = m.Tokens;
    //            }
    //            else
    //            {
    //                groupMethods++;
    //                groupTotal = accumulator;
    //            }
    //            return new { Group = groupId, Method = m };
    //        });
    //        foreach (var batch in batches.GroupBy(b => b.Group))
    //        {
    //            var compressed = await CompressMethods(serviceName + "::" + batch.Key, batch.GroupBy(x => x.Method.Identifier).ToDictionary(x => x.Key, x => x.Last().Method.Node.ToFullString()), model);

    //            foreach (var compressedMethod in compressed)
    //            {
    //                var match = methods.FirstOrDefault(m => m.Identifier == compressedMethod.Key);
    //                if (match != null)
    //                    match.Source = "/*\r\n" + compressedMethod.Value + "\r\n*/";
    //            }
    //            //method.Source = "/*\r\n" + completion.Value.Content.Last().Text.Replace("```pseudocode", string.Empty).Replace("```", string.Empty) + "\r\n*/";
    //        }

    //        // process the ones that failed
    //        int unprocessedCycle = 0;
    //        while (methods.Where(m => m.Source is not null && !m.Source.StartsWith("/*")).ToArray() is { Length: > 0 } unprocessedMethods)
    //        {
    //            unprocessedCycle++;
    //            var compressed = await CompressMethods(serviceName + "::unprocessed::" + unprocessedCycle, unprocessedMethods.GroupBy(x => x.Identifier).ToDictionary(x => x.Key, x => x.Last().Node.ToFullString()), model);
    //            foreach (var compressedMethod in compressed)
    //            {
    //                var match = methods.FirstOrDefault(m => m.Identifier == compressedMethod.Key);
    //                if (match != null)
    //                    match.Source = "/*\r\n" + compressedMethod.Value + "\r\n*/";
    //            }

    //            if (unprocessedCycle > 10) // arbitrary
    //            {
    //                break;
    //            }
    //        }
    //        return methods;
    //    }

    //    /// <summary>
    //    /// Compress methods to replace them with summary of what each method does
    //    /// </summary>
    //    async Task<Dictionary<string, string>> CompressMethods(string callIdentifier, Dictionary<string, string> methods, AIModelDefinition model)
    //    {
    //        //var options = new AzureOpenAIClientOptions();
    //        //options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(5);
    //        //OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);
    //        var completion = await openAIService.CompleteChatAsync(new ChatMessage[]
    //            {
    //        ChatMessage.CreateSystemMessage("""
				//You are a code analysis assistant. When supplied with implementation of a method body from ERP system,
				//you generate summary of what logic in this method does, aimed at software developer audience.
				//Be concise and avoid unnecessary and meaningless statements - your goal is to summarise logic overall and functionality only, not line by line.
				//Identify most important logic in the method and summarise it only. Spurious initialisations and defaults are to only be included if they are relevant and key to understanding algorithms within.
				//Your summary should be no longer than 1000 tokens and as short as possible. Do not duplicate any code from the method. Just provide brief and concise summary.
				//Aim for as short a summary as possible. Ideally summary should just construe 3-5 sentences.
				//Only summarise method body, do not copy declaration - it is only there as a reference.
				
				//Do not format your response with Markdown code block. Just return summary without any extra blocks surrounding it.
				
				//Your response should come as JSON object with keys being method names and values the method summary, e.g.
				//{
				//	"method1": "Generate sample data for concatenation of parameter1 and parameter2"
				//}
				
				//For methods that have no implementation generate empty string as summary. Do not include original code in your response (if absolutely necessary, verbally summarise it) - it should be just a text description of what a given method does.
				//Remember - summarise every method that has implementation that was provided by user.
				
				//Use will supply methods in the following format:
				//### <MethodName> ###
				//<method implementation code>
				//"""),
    //        ChatMessage.CreateUserMessage(string.Join("\r\n\r\n",methods.Select(b => $"### {b.Key} ###\r\n{b.Value}"))),
    //            }, new ChatCompletionOptions
    //            {
    //                Temperature = 0.0f,
    //                //MaxTokens = 16000,
    //                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
    //            });

    //        var jsonData = JsonDocument.Parse(completion.Value.Content.Last().Text).RootElement.EnumerateObject().Select(token => new { Name = token.Name, Summary = token.Value.GetString() }).ToArray();

    //        return jsonData.GroupBy(x => x.Name).ToDictionary(x => x.Key, y => y.Last().Summary);
    //    }
    }
}
