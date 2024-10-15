using Azure.AI.OpenAI;
using BOEmbeddingService.Interfaces;
using Microsoft.CodeAnalysis;
using OpenAI.Chat;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using BOEmbeddingService.Models;
using Microsoft.CodeAnalysis.CSharp;
using System.ClientModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using System.Globalization;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Serilog;
using System.IO;

namespace BOEmbeddingService.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        Serilog.Core.Logger _logger = LoggerService.GetInstance();
        List<string> files = new List<string>();
        Uri gitRepo = new Uri("https://epicor-corp.visualstudio.com/DefaultCollection/");
        const string openAiEndpoint = @"https://hb-dev-openai.openai.azure.com";
        const string openAiEmbeddingModelName = "text-embedding-3-small";
        //AzureKeyCredential openAiKey = new("");
        ApiKeyCredential openAiKey = new ApiKeyCredential("92bf567ccd344dccb7c35d0bb1567dd6");
        string targetDir = Path.GetDirectoryName(@"D:\Epicor\CrawledFiles\new");

        
        AIModelDefinition gpt_4o_mini = new("gpt-4o-mini", 0.000165m / 1000, 0.00066m / 1000);
        AIModelDefinition gpt_4o = new("gpt-4o", 0.00275m / 1000, 0.011m / 1000);

        public EmbeddingService() { 
        }

        public async Task GetCompressMethods()
        {
            try
            {
                /********** CHANGE THIS TO SWAP MODELS! ********/
                //var model = gpt_4o_mini;
                var model = gpt_4o;
                /***********************************************/
                //totalCostDumper.Dump("Total Cost");
                Directory.CreateDirectory(targetDir);
                var codeFileTargetDir = Path.Combine(targetDir, "CompressedCodeFiles");
                Directory.CreateDirectory(codeFileTargetDir);

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

                var items = await GetFiles(@"D:\Epicor\BOObjects");
                var contractFiles = await GetFiles(@"D:\Epicor\BOContracts");

                // skip root folder
                foreach (var boRoot in items/*.Skip(1)*/) //.Where(x => x.Path.EndsWith("APInvoice")))
                {
                    FileInfo fi = new FileInfo(boRoot);

                    var boName = fi.Directory.Name;//Path.GetFileName(boRoot/*boRoot.Path*/);
                    var serviceName = $"ERP.BO.{boName}Svc";
                    var destinationFile = Path.Combine(targetDir, "BusinessObjectDescription", model.DeploymentName, serviceName + ".json");
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                    if (Path.Exists(destinationFile))
                        // skip if file already exists
                        continue;

                    var aiContextFiles = new List<CodeFile>();

                    //var boFiles = await gitClient.GetItemsAsync("Epicor-PD", "current-kinetic", boRoot.Path, recursionLevel: VersionControlRecursionType.OneLevel);

                    // main service logic overrides
                    var mainCodeFile = boRoot; /*boFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Path) == boName && !x.IsFolder);*/
                    var compressedCodeFile = Path.Combine(codeFileTargetDir, boName, fi.Name./*Path.*/TrimStart('/', '\\'));
                    Directory.CreateDirectory(Path.GetDirectoryName(compressedCodeFile));

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
                            var compressed = await CompressCodeFileAsync(mainContent, boName, 1, model);
                            //compressed.DumpTell();
                            await File.WriteAllTextAsync(compressedCodeFile, compressed);
                            aiContextFiles.Add(new CodeFile { Content = compressed, Filename = Path.GetFileName(mainCodeFile) });
                        }
                        catch (Exception ex) 
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }

                    // contract
                    //var contractFiles = await gitClient.GetItemsAsync("Epicor-PD", "current-kinetic", $"/Source/Shared/Contracts/BO/{boName}", recursionLevel: VersionControlRecursionType.OneLevel);
                    files = new List<string>();
                    

                    var contractInterfaceFile = contractFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == boName + "Contract"/* && !x.IsFolder*/);
                    if(contractInterfaceFile == null)
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
                        contractSummary = await GenerateInterfaceImplementationSummary(content, aiContextFiles.ToDictionary(x => x.Filename, x => x.Content), boName, model);
                        await File.WriteAllTextAsync(contractSummaryFile, System.Text.Json.JsonSerializer.Serialize(contractSummary, new JsonSerializerOptions { WriteIndented = true }));
                    }

                    // generate description with openAI


                    var description = await GenerateServiceDescriptionAsync(serviceName, contractSummary, aiContextFiles, model);
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
                foreach (var descriptionFile in Directory.GetFiles(Path.Combine(targetDir, "BusinessObjectDescription", model.DeploymentName), "*.json"))
                {
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(descriptionFile);

                    var questionFile = Path.Combine(targetDir, "Questions", filenameWithoutExtension + ".questions.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(questionFile));
                    if (File.Exists(questionFile))
                        continue;

                    var descriptionJson = await File.ReadAllTextAsync(descriptionFile);
                    var description = JsonSerializer.Deserialize<BusinessObjectDescription>(descriptionJson);


                    var questions = await GenerateQuestions(description, model, filenameWithoutExtension);
                    await File.WriteAllTextAsync(questionFile, JsonSerializer.Serialize(questions.Select(x => new { question = x.Item1, embedding = x.Item2 }), new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        /// <summary>
        /// Generate questions for RAG
        /// </summary>
        private async Task<List<(string, string)>> GenerateQuestions(BusinessObjectDescription description, AIModelDefinition model, string serviceName)
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

            var options = new AzureOpenAIClientOptions();
            options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(2);
            options.NetworkTimeout = TimeSpan.FromMinutes(10);
            OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);

            var completion = await openAiClient.GetChatClient(model.DeploymentName).CompleteChatAsync(new ChatMessage[]
            {
        ChatMessage.CreateSystemMessage(prompt),
        ChatMessage.CreateUserMessage(description.Description),
            }, new ChatCompletionOptions
            {
                Temperature = 0.0f,
                //MaxTokens = 8000,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            });

            var questions = JsonNode.Parse(completion.Value.Content.Last().Text)["questions"].AsArray().Select(x => x.AsValue().GetValue<string>());

            var embeddings = await openAiClient.GetEmbeddingClient(openAiEmbeddingModelName).GenerateEmbeddingsAsync(questions);
            return questions.Zip(embeddings.Value.Select(x => x.Index)).Select(data => (data.First, data.Second.ToString())).ToList();
        }

        /// <summary>
        /// Helpful guidance for how Kinetic BOs work internally
        /// </summary>
        private string GetKineticBusinessObjectImplementationDetails()
        {
            return """
		Implementation of some methods is split into separate stages in addition to base implementation. The base implementation of some key methods is as follows:
		* GetRows - returns full dataset with parent and child tables based on supplied conditions (known as "where clauses") that are defined in SQL-like syntax
		* GetList - return reduced single table dataset with small subset of fields for the parent table only. This again takes SQL-like where clause and is used for fast searches
		* GetByID - return full dataset for parent record specified by its primary key (one or more fields) and all the corresponding child records
		* GetBySysRowID - return full dataset for parent record specified by the supplied GUID identity key (not primary key!) and all corresponding child records
		* Update - apply changes in supplied dataset (full dataset from GetByID, GetBySysRowID or GetRows) and write them back to the database. This includes updates, creation and deletion.
		
		Base implementation of these methods can be augmented by implementing corresponding Before and After methods. For exmple, BeforeUpdate method runs before base update method.
		BeforeGetRows and AfterGetRows methods are special in that they run for any attempt o retrieve full dataset, so for GetRows, GetByID and GetBySysRowID.
		
		Update has special table specific overrides. For exmaple, if we have dataset with tables Parent and Child, implementation of base update functinality can be extended by
		implementing the following table-specific methods: ParentBeforeUpdate, ParentAfterUpdate, ParentBeforeCreate, ChildAfterDelete, etc. So Update() table specific implemenations have the following:
		* <Table>BeforeCreate - runs before new record is inserted into <Table> by Update
		* <Table>AfterCreate - runs after new record is inserted into <Table> by Update
		* <Table>BeforeUpdate - runs before existing record is updated in <Table> by Update
		* <Table>AfterUpdate - runs after existing record is updated in <Table> by Update
		* <Table>BeforeDelete - runs before existing record is deleted in <Table> by Update
		* <Table>AfterDelete - runs after existing record is updated in <Table> by Update
		
		These implementation extensions allow changing and adjusting how a given service implementation functions.
		Full dataset retrieval methods (GetRows, GetByID, GetBySysRowID) have special functions with postfix "ForeignLink". These are automatically invoked to retrieve the data from associated extra tables.
		For example, OrderHed record might include foreign link to Customer.Name as CustomerName which automatically looks up customer for the current record and populated value of the CustomerName column with Cutomer.Name. This will go into dataset table field/column which is not physically present in the database.
		
		Methods with suffic "GetNew" (e.g. TableGetNew) are used to add a new record to the dataset without committing it to database. They would usually populate defaults.
		Methods with prefix "OnChange" are used to validate changes in a specific field and often are used to calculate related values in dataset. They do not write these changes to database.
		
		Epicor Kinetic ERP uses the following terminology and abbreviations throughout it table and service names:
		* PO - purchase order
		* SO - sales order, often just referred to as order
		* AP - accounts payable
		* AR - accounts receivable
		* GL - general ledger
		* Tran - transaction
		* Hed and Head - header record
		* Dtl, Detail and Line - line record (child of header)
		* Rel and Release - release (child or line)
		""";
        }

        /// <summary>
        /// Create summary of what all individual methods do for use with REST API and final summary generation
        /// </summary>
        async Task<Dictionary<string, string>> GenerateInterfaceImplementationSummary(string interfaceFileContents, Dictionary<string, string> implementationFiles, string boName, AIModelDefinition model)
        {
            string systemPrompt = $$"""
		You are C# code interpreter for Epicor Kinetic ERP system. When user supplies you with interface file and summary of method implementations,
		you analyse these and return information about what each individual interface method does.
		
		{{GetKineticBusinessObjectImplementationDetails()}}
		
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

        decimal totalCost = 0m;

        async Task<string> CompressCodeFileAsync(string fullText, string className, int maxTokens, AIModelDefinition model)
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

                var normalised = await ReduceMethods(methods.ToList(), className, model);
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

        async Task<IList<MethodSourceInfo>> ReduceMethods(IList<MethodSourceInfo> methods, string serviceName, AIModelDefinition model)
        {

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
                var compressed = await CompressMethods(serviceName + "::" + batch.Key, batch.GroupBy(x => x.Method.Identifier).ToDictionary(x => x.Key, x => x.Last().Method.Node.ToFullString()), model);

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
                var compressed = await CompressMethods(serviceName + "::unprocessed::" + unprocessedCycle, unprocessedMethods.GroupBy(x => x.Identifier).ToDictionary(x => x.Key, x => x.Last().Node.ToFullString()), model);
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
        async Task<Dictionary<string, string>> CompressMethods(string callIdentifier, Dictionary<string, string> methods, AIModelDefinition model)
        {
            var options = new AzureOpenAIClientOptions();
            options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(5);
            OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey, options);
            var completion = await openAiClient.GetChatClient(model.DeploymentName).CompleteChatAsync(new ChatMessage[]
                {
            ChatMessage.CreateSystemMessage("""
				You are a code analysis assistant. When supplied with implementation of a method body from ERP system,
				you generate summary of what logic in this method does, aimed at software developer audience.
				Be concise and avoid unnecessary and meaningless statements - your goal is to summarise logic overall and functionality only, not line by line.
				Identify most important logic in the method and summarise it only. Spurious initialisations and defaults are to only be included if they are relevant and key to understanding algorithms within.
				Your summary should be no longer than 1000 tokens and as short as possible. Do not duplicate any code from the method. Just provide brief and concise summary.
				Aim for as short a summary as possible. Ideally summary should just construe 3-5 sentences.
				Only summarise method body, do not copy declaration - it is only there as a reference.
				
				Do not format your response with Markdown code block. Just return summary without any extra blocks surrounding it.
				
				Your response should come as JSON object with keys being method names and values the method summary, e.g.
				{
					"method1": "Generate sample data for concatenation of parameter1 and parameter2"
				}
				
				For methods that have no implementation generate empty string as summary. Do not include original code in your response (if absolutely necessary, verbally summarise it) - it should be just a text description of what a given method does.
				Remember - summarise every method that has implementation that was provided by user.
				
				Use will supply methods in the following format:
				### <MethodName> ###
				<method implementation code>
				"""),
            ChatMessage.CreateUserMessage(string.Join("\r\n\r\n",methods.Select(b => $"### {b.Key} ###\r\n{b.Value}"))),
                }, new ChatCompletionOptions
                {
                    Temperature = 0.0f,
                    //MaxTokens = 16000,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                });

            var jsonData = JsonDocument.Parse(completion.Value.Content.Last().Text).RootElement.EnumerateObject().Select(token => new { Name = token.Name, Summary = token.Value.GetString() }).ToArray();

            return jsonData.GroupBy(x => x.Name).ToDictionary(x => x.Key, y => y.Last().Summary);
        }


        /// <summary>
        /// Use Ai to generate full description of what a give business object does based on supplied source code files
        /// </summary>
        async Task<BusinessObjectDescription> GenerateServiceDescriptionAsync(string serviceName, Dictionary<string, string> interfaceDefinition, IEnumerable<CodeFile> codeFiles, AIModelDefinition model)
        {
            OpenAIClient openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), openAiKey);
            var prompt = $$$"""
		# Instructions
		You are an intelligent coding assistant tasked with analysing source code for Epicor Kinetic business object (service) and generating description of its functionality.
		To formulate your response, only use supplied information in the instructions as well as data sent by user. Do not use any additional sources or knowledge beyond this.
		Today is {{{DateTimeOffset.UtcNow.ToString("s")}}}
		Context will include source code in C# for several related files as well as C#-like code where method implementation has been replaced by its functional summary (in block comment). 
		Bodies of all methods have been replaced by functional description of what the code within is intended to do in a block comment. 
		Treat these as if this is what method actually does and not as comments. So if a method is CreateRecord() { /* creates a new record with fields defaulted */ } assume that this method implementation creates a new record with fields defaulted, etc.
		
		You need to generate a small summary of no more than 3 sentences as well as full description containing no more than a single page of text (4-10 paragraphs) aimed at technical audience but focused on functionality rather than code implementation details.
		
		Full description should not go into details on individual methods and functions and only provide overall functionality description for technical audience. 
		It should be sufficient to describe functional specification of the service and permit recreating it anew with no access to source code.
		When generating description focus on functionality specific to this exact business object only and do not describe generic information that is likely to be common to overall framework and all other business objects.
		Do not mention that some code is generated in your response and if applicable, include values for tolerances and constraints enforced by validations. Format your response to be easy to read.
		
		{{{GetKineticBusinessObjectImplementationDetails()}}}
		
		User will send data in the following format:
		### Interface ###
		JSON definition of the interface in code block. This contains method declarations and corresponding summaries of functionality
		### Implementation ###
		C# implementation with method bodies replaced with corresponding comment blocks
		
		Your response should be JSON file with the following fields:
		* name - service name
		* summary - quick summary
		* description  full description
		""";

            // query Open AI for answer
            var completions = await openAiClient.GetChatClient(model.DeploymentName).CompleteChatAsync(new ChatMessage[]
            {
        new SystemChatMessage(prompt),
        new UserChatMessage($$$"""
			## Interface
			```json
			{{{JsonSerializer.Serialize(interfaceDefinition)}}}
			```
			
			## Implementation
			{{{codeFiles.Select(c => $$"""
				```csharp
				// {{c.Filename}}
				
				{{c.Content}}
				```	
				""")}}}
			""")
            }, new ChatCompletionOptions
            {
                Temperature = 0.0f,
                //MaxTokens = 3000,
                //NumberOfResponses = 1,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            });

            var result = System.Text.Json.JsonSerializer.Deserialize<BusinessObjectDescription>(completions.Value.Content.Last().Text);
            result.Name = serviceName;

            return result;
        }

        async Task<string[]> GetFiles(string path)
        {
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
                        Console.WriteLine(Path.GetFileName(d));
                        GetFiles(d);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return files.ToArray();
        }
    }
}
