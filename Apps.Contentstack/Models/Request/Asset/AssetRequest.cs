using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Asset;

public class AssetRequest
{
    [Display("Asset")]
    [DataSource(typeof(AssetDataHandler))]
    public string AssetId { get; set; }
}