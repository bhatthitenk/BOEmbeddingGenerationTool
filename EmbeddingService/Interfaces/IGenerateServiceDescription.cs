using BOEmbeddingService.Models;

namespace BOEmbeddingService.Interfaces;

public interface IGenerateServiceDescription
{
    Task<BusinessObjectDescription> GenerateServiceDescriptionAsync(string serviceName, Dictionary<string, string> interfaceDefinition, IEnumerable<CodeFile> codeFiles, AIModelDefinition model);
}
