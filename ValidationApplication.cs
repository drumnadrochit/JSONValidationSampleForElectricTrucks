namespace ElectricTruckJsonValidator;
using Json.Schema;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Coordinates argument-based file checks, schema validation, and console output.
/// </summary>
public sealed class ValidationApplication
{
	private readonly JsonSchemaValidator _validator;
	private readonly EvaluationIssuePrinter _issuePrinter;

	/// <summary>
	/// Creates an application coordinator with explicit dependencies.
	/// </summary>
	public ValidationApplication(JsonSchemaValidator validator, EvaluationIssuePrinter issuePrinter)
	{
		_validator = validator;
		_issuePrinter = issuePrinter;
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
			var stopwatch = Stopwatch.StartNew();
			var result = await _validator.ValidateAsync(arguments.JsonFilePath, arguments.SchemaFilePath);
			var persistedResultPath = BuildPersistedResultPath(arguments.JsonFilePath);
			await PersistResultAsync(result, arguments, persistedResultPath);
			stopwatch.Stop();

			Console.WriteLine($"Validation result persisted to: {persistedResultPath}");
			Console.WriteLine($"Elapsed time (validation to persistence): {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

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

	private static string BuildPersistedResultPath(string jsonFilePath)
	{
		var directory = Path.GetDirectoryName(jsonFilePath) ?? Environment.CurrentDirectory;
		var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
		return Path.Combine(directory, $"{fileName}.validation-result.json");
	}

	private static async Task PersistResultAsync(EvaluationResults result, CommandLineArguments arguments, string outputPath)
	{
		var payload = new ValidationPersistencePayload(
			result.IsValid,
			arguments.JsonFilePath,
			arguments.SchemaFilePath,
			DateTimeOffset.UtcNow,
			CollectIssues(result));

		await using var stream = File.Create(outputPath);
		await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}

	private static List<string> CollectIssues(EvaluationResults results)
	{
		var issues = new List<string>();
		CollectIssuesRecursive(results, issues);
		return issues;
	}

	private static void CollectIssuesRecursive(EvaluationResults results, ICollection<string> issues)
	{
		if (results.Errors is { Count: > 0 })
		{
			foreach (var error in results.Errors)
			{
				issues.Add($"{error.Key}: {error.Value}");
			}
		}

		if (results.Details is null || results.Details.Count == 0)
		{
			return;
		}

		foreach (var child in results.Details)
		{
			CollectIssuesRecursive(child, issues);
		}
	}

	private sealed record ValidationPersistencePayload(
		bool IsValid,
		string JsonFilePath,
		string SchemaFilePath,
		DateTimeOffset PersistedAtUtc,
		IReadOnlyList<string> Issues);
}
