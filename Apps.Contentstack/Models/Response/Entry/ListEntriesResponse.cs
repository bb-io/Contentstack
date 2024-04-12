using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.Entry;

public class ListEntriesResponse
{
    public IEnumerable<EntryEntity> Entries { get; set; }
    
    public int Count { get; set; }
}