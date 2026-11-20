using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Response.Asset;
using Newtonsoft.Json;

namespace Tests.Contentstack;

[TestClass]
public class AssetEntityTests
{
    [TestMethod]
    public void AssetResponse_DeserializesVersion()
    {
        const string json = """
                            {
                              "asset": {
                                "uid": "asset-id",
                                "_version": 7
                              }
                            }
                            """;

        var response = JsonConvert.DeserializeObject<AssetResponse>(json, JsonConfig.Settings);

        Assert.IsNotNull(response);
        Assert.AreEqual(7, response.Asset.Version);
    }
}
