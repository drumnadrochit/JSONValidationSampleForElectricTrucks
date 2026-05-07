using ControlNode.Api.Abstractions;
using Validation.Contracts;

namespace ControlNode.Api.Services;

public sealed class ContinueOnNodeFailurePolicy : INodeFailurePolicy
{
    public DistributedValidationNodeResult HandleFailure(string nodeUrl, Exception exception, TimeSpan duration)
    {
        return new DistributedValidationNodeResult(
            ResolveNodeId(nodeUrl),
            false,
            [$"Node call failed: {exception.Message}"],
            duration,
            DateTimeOffset.UtcNow);
    }

    private static string ResolveNodeId(string nodeUrl)
    {
        return Uri.TryCreate(nodeUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : nodeUrl;
    }
}