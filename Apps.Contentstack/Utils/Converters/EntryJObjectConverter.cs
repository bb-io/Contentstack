using Apps.Contentstack.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Utils.Converters;

public class EntryJObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(EntryJObject);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException("Deserialization is not supported for EntryJObject");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var entryJObject = (EntryJObject)value;

        var json = new JObject(entryJObject);

        json.WriteTo(writer);
    }
}