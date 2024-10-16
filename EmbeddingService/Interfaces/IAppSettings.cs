using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
    public interface IAppSettings
    {
        public string openAiEndpoint { get; set; }
        public string openAiKey { get; set; }
        public string openAiEmbeddingModelName { get; set; }
        public string gitRepo { get; set; }
        public string targetDir { get; set; }
        public string BOObjectsLocation { get; set; }
        public string BOContractsLocation { get; set; }
    }
}
