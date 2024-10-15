using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BOEmbeddingService.Models
{
    public class BusinessObjectDescription
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }

    }

}
