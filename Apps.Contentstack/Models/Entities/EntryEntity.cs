using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Entities;

public class EntryEntity
{
    [Display("Entry UID")]
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
    
    public IEnumerable<string> Tags { get; set; }
    
    [Display("In progress")]
    [JsonProperty("_in_progress")]
    public bool InProgress { get; set; }
}