using Validation.Contracts;

namespace ControlNode.Api.Abstractions;

public interface IDistributedValidationCoordinator
{
    Task<DistributedValidationResponse> ValidateAsync(DistributedValidationRequest request, CancellationToken cancellationToken);
}