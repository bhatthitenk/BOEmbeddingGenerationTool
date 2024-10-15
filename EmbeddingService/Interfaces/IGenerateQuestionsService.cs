using BOEmbeddingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
	public interface IGenerateQuestionsService
	{
		/// <summary>
		/// Generate questions for RAG
		/// </summary>
		Task<List<(string, string)>> GenerateQuestions(BusinessObjectDescription description, AIModelDefinition model, string serviceName);
	}
}
