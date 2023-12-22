using Apps.Contentstack.Utils.Converters;
using Contentstack.Management.Core.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models;

[JsonConverter(typeof(EntryJObjectConverter))]
public class EntryJObject : JObject, IEntry
{
    public string Title { get; set; }

    public EntryJObject(JObject obj) : base(obj)
    {
        Title = obj["title"]!.ToString();
    }
}