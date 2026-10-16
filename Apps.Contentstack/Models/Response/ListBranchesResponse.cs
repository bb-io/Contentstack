using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response;

public class ListBranchesResponse
{
    public IEnumerable<BranchEntity> Branches { get; set; } = [];
}
