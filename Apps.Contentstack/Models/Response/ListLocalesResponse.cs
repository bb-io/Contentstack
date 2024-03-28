using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response;

public class ListLocalesResponse
{
    public IEnumerable<LocaleEntity> Locales { get; set; }
}