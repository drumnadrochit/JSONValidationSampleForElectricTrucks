using ValidationNode.Api.Abstractions;

namespace ValidationNode.Api.Services;

public sealed class EnvironmentNodeIdentityProvider : INodeIdentityProvider
{
    private readonly IConfiguration _configuration;

    public EnvironmentNodeIdentityProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetNodeId()
    {
        return _configuration["NodeId"]
            ?? Environment.GetEnvironmentVariable("NODE_ID")
            ?? Environment.MachineName;
    }
}