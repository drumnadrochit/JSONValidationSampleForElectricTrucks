namespace Validation.Contracts;

public sealed record ValidationRequest(
	string JsonDocument,
	string JsonSchema,
	string? CorrelationId,
	IDictionary<string, string>? Metadata);

public sealed record ValidationResponse(
	bool IsValid,
	IReadOnlyList<string> Issues,
	string NodeId,
	DateTimeOffset ProcessedAtUtc,
	string? CorrelationId,
	TimeSpan Duration);

public sealed record DistributedValidationRequest(
	string JsonDocument,
	string JsonSchema,
	int ValidationCount,
	string? CorrelationId,
	IDictionary<string, string>? Metadata);

public sealed record DistributedValidationNodeResult(
	string NodeId,
	bool IsValid,
	IReadOnlyList<string> Issues,
	TimeSpan Duration,
	DateTimeOffset ProcessedAtUtc);

public sealed record DistributedValidationResponse(
	string CorrelationId,
	bool IsValid,
	int RequestedValidations,
	int CompletedValidations,
	IReadOnlyList<DistributedValidationNodeResult> Results,
	TimeSpan TotalDuration);