using Apps.Contentstack.Models.Response.ContentType;

namespace Apps.Contentstack.Models.Entities;

public class ContentTypeEntity
{
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public IEnumerable<SchemaResponse> Schema { get; set; }
}