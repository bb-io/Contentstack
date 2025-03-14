using Apps.Contentstack.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;
using OpenAITests.Base;

namespace Tests.Contentstack;

[TestClass]
public class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidateConnection_WithValidCredentials_ReturnsValid()
    {
        var validator = new ConnectionValidator();

        var result = await validator.ValidateConnection(Creds, CancellationToken.None);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ValidateConnection_WithInvalidCredentials_ReturnsInvalid()
    {
        var validator = new ConnectionValidator();

        var newCreds = Creds.Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));
        var result = await validator.ValidateConnection(newCreds, CancellationToken.None);
        Assert.IsFalse(result.IsValid);
    }
}