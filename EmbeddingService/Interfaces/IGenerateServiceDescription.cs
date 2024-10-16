using BOEmbeddingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
    public interface IGenerateServiceDescription
    {
        Task<BusinessObjectDescription> GenerateServiceDescriptionAsync(string serviceName, Dictionary<string, string> interfaceDefinition, IEnumerable<CodeFile> codeFiles, AIModelDefinition model);
    }
}
