namespace ElectricTruckJsonValidator;

/// <summary>
/// Holds command-line input and parse feedback.
/// </summary>
public sealed class CommandLineArguments
{
	private CommandLineArguments(string jsonFilePath, string schemaFilePath, int parallelCount, bool isValid, IReadOnlyList<string> messages)
	{
		JsonFilePath = jsonFilePath;
		SchemaFilePath = schemaFilePath;
		ParallelCount = parallelCount;
		IsValid = isValid;
		Messages = messages;
	}

	/// <summary>
	/// Gets the input JSON file path.
	/// </summary>
	public string JsonFilePath { get; }

	/// <summary>
	/// Gets the JSON schema file path.
	/// </summary>
	public string SchemaFilePath { get; }

	/// <summary>
	/// Gets the number of parallel validation and serialization runs (default: 1).
	/// </summary>
	public int ParallelCount { get; }

	/// <summary>
	/// Gets a value indicating whether parsing produced usable arguments.
	/// </summary>
	public bool IsValid { get; }

	/// <summary>
	/// Gets user-facing parse messages such as usage text.
	/// </summary>
	public IReadOnlyList<string> Messages { get; }

	/// <summary>
	/// Creates a valid argument instance.
	/// </summary>
	public static CommandLineArguments Valid(string jsonFilePath, string schemaFilePath, int parallelCount = 1)
	{
		return new CommandLineArguments(jsonFilePath, schemaFilePath, parallelCount, true, Array.Empty<string>());
	}

	/// <summary>
	/// Creates an invalid argument instance with user guidance.
	/// </summary>
	public static CommandLineArguments Invalid(IReadOnlyList<string> messages)
	{
		return new CommandLineArguments(string.Empty, string.Empty, 1, false, messages);
	}
}
