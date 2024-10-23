using BOEmbeddingService;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var appSettings = new AppSettings();
configuration.Bind(appSettings);

var loggerService = new LoggerService();
loggerService.GetInstance();
//var appSettings = Configuration.BuildAppSettings();

var serviceprovider = new ServiceCollection()
    // Add Service
    .AddScoped<IGenerateEmbeddingService, GenerateEmbeddingService>()
    .AddScoped<ICommonService, CommonService>()
    .AddScoped<ICompressMethodsService, CompressMethodsService>()
    .AddScoped<IGenerateInterfaceSummaryService, GenerateInterfaceSummaryService>()
    .AddScoped<IGenerateQuestionsService, GenerateQuestionsService>()
    .AddScoped<IGenerateServiceDescription, GenerateServiceDescriptionService>()
    .AddSingleton<AppSettings>(appSettings)
    .AddSingleton<ILoggerService>(loggerService)
    .AddSingleton<IAzureOpenAIService, AzureOpenAIService>()
    .AddSingleton<IMongoDbService, MongoDbService>()
    .BuildServiceProvider();

var embeddingService = serviceprovider.GetRequiredService<IGenerateEmbeddingService>();

await embeddingService.ProcessAndGenerateEmbeddings();

// See https://aka.ms/new-console-template for more information
