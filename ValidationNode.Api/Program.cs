using Json.Schema;
using System.Diagnostics;
using System.Text.Json;
using Validation.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ValidationProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", (IConfiguration configuration) =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        nodeId = GetNodeId(configuration)
    });
});

app.MapPost("/validate", async (ValidationRequest request, ValidationProcessor processor, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.JsonDocument))
    {
        return Results.BadRequest(new { error = "JsonDocument is required." });
    }

    if (string.IsNullOrWhiteSpace(request.JsonSchema))
    {
        return Results.BadRequest(new { error = "JsonSchema is required." });
    }

    var response = await processor.ValidateAsync(request, GetNodeId(configuration), cancellationToken);
    return Results.Ok(response);
})
.WithName("ValidateDocument");

app.Run();

static string GetNodeId(IConfiguration configuration)
{
    return configuration["NodeId"]
        ?? Environment.GetEnvironmentVariable("NODE_ID")
        ?? Environment.MachineName;
}

internal sealed class ValidationProcessor
{
    public Task<ValidationResponse> ValidateAsync(ValidationRequest request, string nodeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        var schema = JsonSchema.FromText(request.JsonSchema);

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
