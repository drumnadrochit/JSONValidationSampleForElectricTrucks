using Validation.Contracts;

namespace ControlNode.Api.Abstractions;

public interface INodeFailurePolicy
{
    DistributedValidationNodeResult HandleFailure(string nodeUrl, Exception exception, TimeSpan duration);
}