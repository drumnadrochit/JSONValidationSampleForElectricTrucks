namespace ControlNode.Api.Options;

public sealed class ValidationNodeOptions
{
    public const string SectionName = "ValidationNodes";

    public string[] Urls { get; init; } = Array.Empty<string>();
}