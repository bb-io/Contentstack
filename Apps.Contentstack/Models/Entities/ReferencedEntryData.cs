using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models.Entities;

public record ReferencedEntryData(string ContentTypeId, string EntryId, JObject Entry, ContentTypeBlockEntity Schema);
