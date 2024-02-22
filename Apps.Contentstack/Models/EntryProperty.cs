using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json;

namespace Apps.Contentstack.Models;

public class EntryProperty
{
    public string Uid { get; set; }
    
    [JsonProperty("data_type")]
    public string DataType { get; set; }
    
    [JsonProperty("non_localizable")]
    public bool NonLocalizable { get; set; }
    
    public List<ContentTypeContentEntity>? Blocks { get; set; }
}