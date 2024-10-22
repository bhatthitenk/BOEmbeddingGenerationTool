using System.Text.Json.Serialization;

namespace BOEmbeddingService.Models;

public class BusinessObjectDescription
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }

}
