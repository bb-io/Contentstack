using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models;

public class EntryProperty
{
    public string Uid { get; set; }
    
    [JsonProperty("data_type")]
    public string DataType { get; set; }

    [JsonProperty("multiple")]
    public bool Multiple { get; set; }
    
    [JsonProperty("non_localizable")]
    public bool NonLocalizable { get; set; }
    
    public List<ContentTypeBlockEntity>? Blocks { get; set; }
    
    public JArray? Schema { get; set; }

    public JObject? ContentTypeSchema { get; set; } = new();
}