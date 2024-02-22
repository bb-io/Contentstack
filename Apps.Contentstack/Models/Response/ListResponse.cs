namespace Apps.Contentstack.Models.Response;

public class ListResponse<T>
{
    public virtual IEnumerable<T> Items { get; set; }
}