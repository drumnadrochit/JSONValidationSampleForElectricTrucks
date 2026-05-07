using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using Validation.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<ValidationNodeOptions>(builder.Configuration.GetSection(ValidationNodeOptions.SectionName));
builder.Services.AddSingleton<RoundRobinNodeSelector>();
builder.Services.AddHttpClient<ValidationNodeClient>();
builder.Services.AddSingleton<ControlPlaneValidationCoordinator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", role = "control-node" }));

app.MapPost("/distributed-validation", async (DistributedValidationRequest request, ControlPlaneValidationCoordinator coordinator, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.JsonDocument))
    {
        return Results.BadRequest(new { error = "JsonDocument is required." });
    }

    if (string.IsNullOrWhiteSpace(request.JsonSchema))
    {
        return Results.BadRequest(new { error = "JsonSchema is required." });
    }

    if (request.ValidationCount < 1)
    {
        return Results.BadRequest(new { error = "ValidationCount must be at least 1." });
    }

    var response = await coordinator.ValidateAsync(request, cancellationToken);
    return Results.Ok(response);
})
.WithName("RunDistributedValidation");

app.Run();

internal sealed class ControlPlaneValidationCoordinator
{
    private readonly ValidationNodeClient _client;
    private readonly RoundRobinNodeSelector _selector;

    public ControlPlaneValidationCoordinator(ValidationNodeClient client, RoundRobinNodeSelector selector)
    {
        _client = client;
        _selector = selector;
    }

    public async Task<DistributedValidationResponse> ValidateAsync(DistributedValidationRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;

        var tasks = Enumerable.Range(0, request.ValidationCount)
            .Select(async _ =>
            {
                var nodeUrl = _selector.NextNode();
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
            });

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
}

internal sealed class ValidationNodeClient
{
    private readonly HttpClient _httpClient;

    public ValidationNodeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ValidationResponse> ValidateAsync(string nodeUrl, ValidationRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"{nodeUrl.TrimEnd('/')}/validate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ValidationResponse>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException($"Validation node {nodeUrl} returned an empty response.");
        }

        return payload;
    }
}

internal sealed class RoundRobinNodeSelector
{
    private readonly string[] _nodes;
    private int _currentIndex = -1;

    public RoundRobinNodeSelector(IConfiguration configuration)
    {
        _nodes = configuration
            .GetSection(ValidationNodeOptions.SectionName)
            .Get<ValidationNodeOptions>()?.Urls?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToArray()
            ?? Array.Empty<string>();

        if (_nodes.Length == 0)
        {
            throw new InvalidOperationException("At least one validation node URL must be configured.");
        }
    }

    public string NextNode()
    {
        var index = Interlocked.Increment(ref _currentIndex);
        return _nodes[index % _nodes.Length];
    }
}

internal sealed class ValidationNodeOptions
{
    public const string SectionName = "ValidationNodes";

    public string[] Urls { get; init; } = Array.Empty<string>();
}
