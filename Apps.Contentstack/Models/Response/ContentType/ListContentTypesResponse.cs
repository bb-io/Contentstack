using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.ContentType;

public class ListContentTypesResponse
{
    public IEnumerable<ContentTypeEntity> ContentTypes { get; set; }
}