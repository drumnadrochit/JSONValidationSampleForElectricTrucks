using Validation.Contracts;

namespace ControlNode.Api.Abstractions;

public interface IValidationNodeClient
{
    Task<ValidationResponse> ValidateAsync(string nodeUrl, ValidationRequest request, CancellationToken cancellationToken);
}