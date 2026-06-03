using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Entry;

public class EntryLocaleItem
{
    [Display("Locale code")]
    public string Code { get; set; } = string.Empty;

    [Display("Is localized")]
    public bool Localized { get; set; }
}

public class GetEntryLocalesResponse
{
    [Display("Locales")]
    public IEnumerable<EntryLocaleItem> Locales { get; set; } = [];
}
