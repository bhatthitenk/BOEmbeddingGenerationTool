using BOEmbeddingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
	public interface IGenerateInterfaceSummaryService
	{
		Task GenerateInterfaceSummary();
		Task<Dictionary<string, string>> GenerateInterfaceImplementationSummary(string interfaceFileContents, Dictionary<string, string> implementationFiles, string boName, AIModelDefinition model);
	}
}
