using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models.Entities;

public class ContentTypeContentEntity
{
    public string Uid { get; set; }

    public string Title { get; set; }

    public JArray Schema { get; set; }
}