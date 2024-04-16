using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class CalculateEntriesRequest
{
    [Display("Content types"), DataSource(typeof(ContentTypeDataHandler))]
    public IEnumerable<string>? ContentTypes { get; set; }
    
    [Display("Workflow stages"), DataSource(typeof(WorkflowStageDataHandler))]
    public IEnumerable<string>? WorkflowStages { get; set; }
}