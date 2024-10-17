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
        private readonly ILoggerService _loggerService;

        //AIModelDefinition gpt_4o_mini = new("gpt-4o-mini", 0.000165m / 1000, 0.00066m / 1000);
        //AIModelDefinition gpt_4o = new("gpt-4o", 0.00275m / 1000, 0.011m / 1000);

        private readonly ICommonService _commonService;
		private readonly ICompressMethodsService _compressMethodsService;
		private readonly IGenerateInterfaceSummaryService _generateInterfaceSummaryService;
		private readonly IGenerateQuestionsService _generateQuestionsService;
        private readonly IGenerateServiceDescription _generateServiceDescription;

		public EmbeddingService(ICommonService commonService,
			ICompressMethodsService compressMethodsService,
			IGenerateInterfaceSummaryService generateInterfaceSummaryService, 
            IGenerateQuestionsService generateQuestionsService,
            IGenerateServiceDescription generateServiceDescription, IAppSettings appSettings, ILoggerService loggerService)
		{
            _appSettings = appSettings;
            _loggerService = loggerService;
			_commonService = commonService;
            _compressMethodsService = compressMethodsService;
			_generateInterfaceSummaryService = generateInterfaceSummaryService;
			_generateQuestionsService = generateQuestionsService;
            _generateServiceDescription = generateServiceDescription;

        }

		public async Task EmbeddedBOObjects()
        {
            try
            {

                var contractDefinitionTargetDir = Path.Combine(_appSettings.targetDir, "ContractSummaries");
                Directory.CreateDirectory(contractDefinitionTargetDir);

				var items = await _commonService.GetFiles(_appSettings.BOObjectsLocation);
                var contractFiles = await _commonService.GetFiles(_appSettings.BOContractsLocation);

                //CompressMethods
                Console.WriteLine("##############################################################");
                Console.WriteLine("                 Compression Starts                           ");
                Console.WriteLine("##############################################################");
                
                await _compressMethodsService.GetCompressMethods();
                
                Console.WriteLine("");
                Console.WriteLine("##############################################################");
                Console.WriteLine("                 Compression Ends                             ");
                Console.WriteLine("##############################################################");

                //GeneraatContract
                Console.WriteLine("");
                Console.WriteLine("##############################################################");
                Console.WriteLine("         Compression Summary Generation Starts                ");
                Console.WriteLine("##############################################################");
                
                await _generateInterfaceSummaryService.GenerateInterfaceSummary();

                Console.WriteLine("");
                Console.WriteLine("##############################################################");
                Console.WriteLine("         Compression Summary Generation Ends                  ");
                Console.WriteLine("##############################################################");

                //Generate Questions
                Console.WriteLine("");
                Console.WriteLine("##############################################################");
                Console.WriteLine("             Question Generation Starts                       ");
                Console.WriteLine("##############################################################");
                
                await _generateQuestionsService.GenerateQuestions();

                Console.WriteLine("");
                Console.WriteLine("##############################################################");
                Console.WriteLine("              Question Generation Ends                        ");
                Console.WriteLine("##############################################################");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }
}
