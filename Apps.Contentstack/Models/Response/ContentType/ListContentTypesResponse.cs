using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Response.ContentType;

public class ListContentTypesResponse : ListResponse<ContentTypeEntity>
{
    [JsonProperty("content_types")]
    public override IEnumerable<ContentTypeEntity> Items { get; set; }
}