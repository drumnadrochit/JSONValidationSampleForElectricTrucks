using ValidationNode.Api.Abstractions;
using ValidationNode.Api.Services;
using Validation.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<INodeIdentityProvider, EnvironmentNodeIdentityProvider>();
builder.Services.AddSingleton<ISchemaCache, InMemorySchemaCache>();
builder.Services.AddSingleton<IValidationService, JsonSchemaValidationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", (INodeIdentityProvider identityProvider) =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        nodeId = identityProvider.GetNodeId()
    });
});

app.MapPost("/validate", async (ValidationRequest request, IValidationService validator, INodeIdentityProvider identityProvider, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.JsonDocument))
    {
        return Results.BadRequest(new { error = "JsonDocument is required." });
    }

    if (string.IsNullOrWhiteSpace(request.JsonSchema))
    {
        return Results.BadRequest(new { error = "JsonSchema is required." });
    }

    var response = await validator.ValidateAsync(request, identityProvider.GetNodeId(), cancellationToken);
    return Results.Ok(response);
})
.WithName("ValidateDocument");

app.Run();
