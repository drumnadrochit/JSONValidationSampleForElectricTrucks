using ControlNode.Api.Abstractions;
using ControlNode.Api.Options;
using ControlNode.Api.Services;
using Validation.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<ValidationNodeOptions>(builder.Configuration.GetSection(ValidationNodeOptions.SectionName));
builder.Services.AddSingleton<INodeSelector, RoundRobinNodeSelector>();
builder.Services.AddSingleton<INodeFailurePolicy, ContinueOnNodeFailurePolicy>();
builder.Services.AddHttpClient<IValidationNodeClient, ValidationNodeHttpClient>();
builder.Services.AddSingleton<IDistributedValidationCoordinator, DistributedValidationCoordinator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", role = "control-node" }));

app.MapPost("/distributed-validation", async (DistributedValidationRequest request, IDistributedValidationCoordinator coordinator, CancellationToken cancellationToken) =>
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
