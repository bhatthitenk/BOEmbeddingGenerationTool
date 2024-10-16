using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BOEmbeddingService.Services
{
	public class CompressMethodsService : ICompressMethodsService
	{
		private readonly ICommonService _commonService;
		OpenAIService openAIService = new OpenAIServiceBuilder().Build();

		public CompressMethodsService(ICommonService commonService)
		{
			_commonService = commonService;
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

				var items = await _commonService.GetFiles(_appSettings.BOObjectsLocation);
				var contractFiles = await _commonService.GetFiles(_appSettings.BOContractsLocation);

				// skip root folder
				foreach (var boRoot in items/*.Skip(1)*/) //.Where(x => x.Path.EndsWith("APInvoice")))
				{
					FileInfo fi = new FileInfo(boRoot);

					var boName = fi.Directory.Name;//Path.GetFileName(boRoot/*boRoot.Path*/);
					var serviceName = $"ERP.BO.{boName}Svc";
					var destinationFile = Path.Combine(targetDir, "BusinessObjectDescription", openAIService.Model.DeploymentName, serviceName + ".json");
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
							var compressed = await CompressCodeFileAsync(mainContent, boName, 1, openAIService.Model);
							//compressed.DumpTell();
							await File.WriteAllTextAsync(compressedCodeFile, compressed);
							aiContextFiles.Add(new CodeFile { Content = compressed, Filename = Path.GetFileName(mainCodeFile) });
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.ToString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
