using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Models
{
    public class AIModelDefinition
    {
        public AIModelDefinition(string deploymentName, decimal inputCostPerToken, decimal outputCostPerToken) 
        {
            DeploymentName = deploymentName;
            InputCostPerToken = inputCostPerToken;
            OutputCostPerToken = outputCostPerToken;
        }
        public string DeploymentName { get; set; }
        public decimal InputCostPerToken { get; set; }
        public decimal OutputCostPerToken { get; set; }
    }
}
