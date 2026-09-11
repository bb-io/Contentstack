using Tests.Contentstack.Base;
using Apps.Contentstack.Connections;
using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Contentstack;

[TestClass]
public class ConnectionValidatorTests : TestBaseMultipleConnections
{
    [TestMethod, TargetConnections]
    public async Task ValidateConnection_WithValidCredentials_ReturnsValid(InvocationContext invocationContext)
    {
        // Arrange
        var validator = new ConnectionValidator();
        var credentials = invocationContext.AuthenticationCredentialsProviders
            .Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value));

        // Act
        var result = await validator.ValidateConnection(credentials, CancellationToken.None);

        // Assert
        TestContext.WriteLine(result.Message);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod, TargetConnections]
    public async Task ValidateConnection_WithInvalidCredentials_ReturnsInvalid(InvocationContext invocationContext)
    {
        // Arrange
        var validator = new ConnectionValidator();
    
        var newCredentials = invocationContext.AuthenticationCredentialsProviders
            .Select(x => new AuthenticationCredentialsProvider(
                x.KeyName,
                x.KeyName == CredsNames.ConnectionType ? x.Value : x.Value + "_incorrect"));
    
        // Act
        var result = await validator.ValidateConnection(newCredentials, CancellationToken.None);
    
        // Assert
        TestContext.WriteLine(result.Message);
        Assert.IsFalse(result.IsValid);
    }
}