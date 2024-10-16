using Microsoft.Extensions.DependencyInjection;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Services;
using Microsoft.Extensions.Configuration;
using BOEmbeddingService;

var serviceprovider = new ServiceCollection()
    // Add Service
    .AddScoped<IEmbeddingService, EmbeddingService>()
    .AddScoped<ICommonService, CommonService>()
    .AddScoped<IGenerateInterfaceSummaryService, GenerateInterfaceSummaryService>()
    .AddScoped<IGenerateQuestionsService, GenerateQuestionsService>()
    .AddScoped<IGenerateServiceDescription, GenerateServiceDescriptionService>()
    .AddSingleton<appSettings>()
	.BuildServiceProvider();

var appSettings = Configuration.BuildAppSettings();
var embeddingService = serviceprovider.GetRequiredService<IEmbeddingService>();

await embeddingService.EmbeddedBOObjects();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
