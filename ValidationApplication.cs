namespace ElectricTruckJsonValidator;

/// <summary>
/// Coordinates argument-based file checks, schema validation, and console output.
/// </summary>
public sealed class ValidationApplication
{
	private readonly IJsonSchemaValidator _validator;
	private readonly IEvaluationIssuePrinter _issuePrinter;

	/// <summary>
	/// Creates an application coordinator with explicit dependencies.
	/// </summary>
	public ValidationApplication(IJsonSchemaValidator validator, IEvaluationIssuePrinter issuePrinter)
	{
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_issuePrinter = issuePrinter ?? throw new ArgumentNullException(nameof(issuePrinter));
	}

	/// <summary>
	/// Runs the full validation workflow and returns the process exit code.
	/// </summary>
	/// <param name="arguments">Validated command-line arguments.</param>
	/// <returns><c>0</c> for success, otherwise <c>1</c>.</returns>
	public async Task<int> RunAsync(CommandLineArguments arguments)
	{
		if (!File.Exists(arguments.JsonFilePath))
		{
			Console.WriteLine($"JSON file was not found: {arguments.JsonFilePath}");
			return 1;
		}

		if (!File.Exists(arguments.SchemaFilePath))
		{
			Console.WriteLine($"JSON schema file was not found: {arguments.SchemaFilePath}");
			return 1;
		}

		try
		{
			var result = await _validator.ValidateAsync(arguments.JsonFilePath, arguments.SchemaFilePath);

			if (result.IsValid)
			{
				Console.WriteLine("Validation succeeded: JSON is valid against the schema.");
				return 0;
			}

			Console.WriteLine("Validation failed: JSON does not match the schema.");
			_issuePrinter.Print(result);
			return 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Validation error: {ex.Message}");
			return 1;
		}
	}
}
