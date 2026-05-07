using Json.Schema;
using System.Diagnostics;
using System.Text.Json;
using ValidationNode.Api.Abstractions;
using Validation.Contracts;

namespace ValidationNode.Api.Services;

public sealed class JsonSchemaValidationService : IValidationService
{
    private readonly ISchemaCache _schemaCache;

    public JsonSchemaValidationService(ISchemaCache schemaCache)
    {
        _schemaCache = schemaCache;
    }

    public Task<ValidationResponse> ValidateAsync(ValidationRequest request, string nodeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        var schema = _schemaCache.GetOrAdd(request.JsonSchema);

        using var jsonDocument = JsonDocument.Parse(request.JsonDocument);
        var instance = jsonDocument.RootElement.Clone();
        var result = schema.Evaluate(instance, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        stopwatch.Stop();

        return Task.FromResult(new ValidationResponse(
            result.IsValid,
            CollectIssues(result),
            nodeId,
            DateTimeOffset.UtcNow,
            request.CorrelationId,
            stopwatch.Elapsed));
    }

    private static IReadOnlyList<string> CollectIssues(EvaluationResults results)
    {
        var issues = new List<string>();
        CollectIssuesRecursive(results, issues);
        return issues;
    }

    private static void CollectIssuesRecursive(EvaluationResults results, ICollection<string> issues)
    {
        if (results.Errors is { Count: > 0 })
        {
            foreach (var error in results.Errors)
            {
                issues.Add($"{error.Key}: {error.Value}");
            }
        }

        if (results.Details is null || results.Details.Count == 0)
        {
            return;
        }

        foreach (var child in results.Details)
        {
            CollectIssuesRecursive(child, issues);
        }
    }
}