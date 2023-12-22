using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.Asset;

public class ListAssetsResponse
{
    public IEnumerable<AssetEntity> Assets { get; set; }
}