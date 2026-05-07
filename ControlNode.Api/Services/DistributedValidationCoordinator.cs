using ControlNode.Api.Abstractions;
using System.Diagnostics;
using Validation.Contracts;

namespace ControlNode.Api.Services;

public sealed class DistributedValidationCoordinator : IDistributedValidationCoordinator
{
    private readonly IValidationNodeClient _client;
    private readonly INodeSelector _selector;
    private readonly INodeFailurePolicy _failurePolicy;

    public DistributedValidationCoordinator(IValidationNodeClient client, INodeSelector selector, INodeFailurePolicy failurePolicy)
    {
        _client = client;
        _selector = selector;
        _failurePolicy = failurePolicy;
    }

    public async Task<DistributedValidationResponse> ValidateAsync(DistributedValidationRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;

        var tasks = Enumerable.Range(0, request.ValidationCount)
            .Select(_ => ValidateOnNodeAsync(request, correlationId, cancellationToken));

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        return new DistributedValidationResponse(
            correlationId,
            results.All(result => result.IsValid),
            request.ValidationCount,
            results.Length,
            results,
            stopwatch.Elapsed);
    }

    private async Task<DistributedValidationNodeResult> ValidateOnNodeAsync(DistributedValidationRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var nodeUrl = _selector.NextNode();
        var nodeStopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _client.ValidateAsync(nodeUrl, new ValidationRequest(
                request.JsonDocument,
                request.JsonSchema,
                correlationId,
                request.Metadata), cancellationToken);

            return new DistributedValidationNodeResult(
                response.NodeId,
                response.IsValid,
                response.Issues,
                response.Duration,
                response.ProcessedAtUtc);
        }
        catch (Exception ex)
        {
            nodeStopwatch.Stop();
            return _failurePolicy.HandleFailure(nodeUrl, ex, nodeStopwatch.Elapsed);
        }
    }
}