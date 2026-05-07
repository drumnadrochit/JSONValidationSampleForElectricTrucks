using Validation.Contracts;

namespace ValidationNode.Api.Abstractions;

public interface IValidationService
{
    Task<ValidationResponse> ValidateAsync(ValidationRequest request, string nodeId, CancellationToken cancellationToken);
}