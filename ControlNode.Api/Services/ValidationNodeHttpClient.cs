using ControlNode.Api.Abstractions;
using System.Net.Http.Json;
using Validation.Contracts;

namespace ControlNode.Api.Services;

public sealed class ValidationNodeHttpClient : IValidationNodeClient
{
    private readonly HttpClient _httpClient;

    public ValidationNodeHttpClient(HttpClient httpClient)
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