using BOEmbeddingService.Models;
namespace BOEmbeddingService;

public class AppSettings
{
    public string AzureOpenAIEndpoint { get; set; }
    public string AzureOpenAIKey { get; set; }
    public string AzureOpenAIChatModelName { get; set; }
    public string AzureOpenAIEmbeddingModelName { get; set; }
    public string GitRepo { get; set; }
    public string TargetDir { get; set; }
    public string BOObjectsLocation { get; set; }
    public string BOContractsLocation { get; set; }
    public bool SkipCompression { get; set; }
    public int SkipCompressionFileSizeInBytes { get; set; }
    public MongoDbSettings MongoDbSettings { get; set; }
}
