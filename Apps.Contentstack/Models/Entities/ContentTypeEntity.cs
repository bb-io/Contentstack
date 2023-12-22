using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models.Entities;

public class ContentTypeEntity
{
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public JArray Schema { get; set; }
}