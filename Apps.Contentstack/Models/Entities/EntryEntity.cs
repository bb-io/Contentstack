using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Entities;

public class EntryEntity
{
    [Display("Entry ID")]
    public string Uid { get; set; } = string.Empty;

    [Display("Locale")]
    [JsonProperty("locale")]
    public string Locale { get; set; } = string.Empty;

    [Display("Entry title")]
    public string Title { get; set; } = string.Empty;
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }

    [DefinitionIgnore, JsonProperty("_version")]
    public int Version { get; set; }
    
    public IEnumerable<string> Tags { get; set; } = new List<string>();
    
    [Display("In progress")]
    [JsonProperty("_in_progress")]
    public bool InProgress { get; set; }
    
    [JsonProperty("_workflow")]
    public WorkflowStageEntity? Workflow { get; set; }
}