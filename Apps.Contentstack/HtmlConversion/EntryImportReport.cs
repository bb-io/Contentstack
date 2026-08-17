namespace Apps.Contentstack.HtmlConversion;

public class EntryImportReport
{
    public List<string> Errors { get; } = [];

    public List<string> CreatedFields { get; } = [];

    public void Add(EntryImportReport other)
    {
        Errors.AddRange(other.Errors);
        CreatedFields.AddRange(other.CreatedFields);
    }
}
