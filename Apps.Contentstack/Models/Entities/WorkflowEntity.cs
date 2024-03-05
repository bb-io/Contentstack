using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Entities;

public class WorkflowEntity
{
    public string Uid { get; set; }
    
    public string Name { get; set; }
    
    public string Branch { get; set; }
    
    public int Version { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
    
    public IEnumerable<WorkflowStageEntity> WorkflowStages { get; set; }
}