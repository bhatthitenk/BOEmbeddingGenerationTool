using BOEmbeddingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
	public interface ICommonService
	{
		Task<string[]> GetFiles(string path);

		Task<string> GetKineticBusinessObjectImplementationDetails();
	}
}
