using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService
{
    public class Configuration
    {
        public static appSettings BuildAppSettings()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appSettings.json", false, false).Build();

            var settings = new appSettings()
            {
                openAiEndpoint = configuration["openAiEndpoint"],
                openAiKey = configuration["openAiKey"],
                openAiEmbeddingModelName = configuration["openAiEmbeddingModelName"],
                gitRepo = configuration["gitRepo"],
                targetDir = configuration["targetDir"],
                BOObjectsLocation = configuration["BOObjectsLocation"],
                BOContractsLocation = configuration["BOContractsLocation"]
            };
             
            return settings; 
        }
    }
}
