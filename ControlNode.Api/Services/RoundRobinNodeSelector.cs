using ControlNode.Api.Abstractions;
using ControlNode.Api.Options;
using Microsoft.Extensions.Options;

namespace ControlNode.Api.Services;

public sealed class RoundRobinNodeSelector : INodeSelector
{
    private readonly string[] _nodes;
    private int _currentIndex = -1;

    public RoundRobinNodeSelector(IOptions<ValidationNodeOptions> options)
    {
        _nodes = options.Value.Urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToArray();

        if (_nodes.Length == 0)
        {
            throw new InvalidOperationException("At least one validation node URL must be configured.");
        }
    }

    public string NextNode()
    {
        var index = Interlocked.Increment(ref _currentIndex);
        var normalizedIndex = (int)((uint)index % (uint)_nodes.Length);
        return _nodes[normalizedIndex];
    }
}