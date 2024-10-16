using Microsoft.Extensions.DependencyInjection;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Services;
using BOEmbeddingService;
using Microsoft.Extensions.Configuration;


IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appSettings.json", false, false).Build();

var appSettings = new AppSettings();
configuration.Bind(appSettings);

var loggerService = new LoggerService();
loggerService.GetInstance();
//var appSettings = Configuration.BuildAppSettings();

var serviceprovider = new ServiceCollection()
    // Add Service
    .AddScoped<IEmbeddingService, EmbeddingService>()
    .AddScoped<ICommonService, CommonService>()
    .AddScoped<IGenerateInterfaceSummaryService, GenerateInterfaceSummaryService>()
    .AddScoped<IGenerateQuestionsService, GenerateQuestionsService>()
    .AddScoped<IGenerateServiceDescription, GenerateServiceDescriptionService>()
    .AddSingleton<IAppSettings>(appSettings)
    .AddSingleton<ILoggerService>(loggerService)
    .BuildServiceProvider();

var embeddingService = serviceprovider.GetRequiredService<IEmbeddingService>();

await embeddingService.EmbeddedBOObjects();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
