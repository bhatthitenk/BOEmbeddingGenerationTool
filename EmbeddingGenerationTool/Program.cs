using Microsoft.Extensions.DependencyInjection;
using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Services;

var serviceprovider = new ServiceCollection()
    // Add Service
    .AddScoped<IEmbeddingService, EmbeddingService>()
    .BuildServiceProvider();

var embeddingService = serviceprovider.GetRequiredService<IEmbeddingService>();

await embeddingService.GetCompressMethods();

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
