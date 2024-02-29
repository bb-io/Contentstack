using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.ContentType;

public class ListContentTypesContentResponse
{
    public IEnumerable<ContentTypeBlockEntity> ContentTypes { get; set; }
}