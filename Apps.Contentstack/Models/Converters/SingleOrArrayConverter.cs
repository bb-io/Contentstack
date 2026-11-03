using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models.Converters;

public class SingleOrArrayConverter<T> : JsonConverter<List<T>>
{
    public override List<T> ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        return token.Type switch
        {
            JTokenType.Array => token.ToObject<List<T>>(serializer) ?? [],
            JTokenType.Object => [token.ToObject<T>(serializer)!],
            _ => []
        };
    }

    public override void WriteJson(JsonWriter writer, List<T>? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);
}
