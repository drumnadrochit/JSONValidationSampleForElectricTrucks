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

			if (arguments.ParallelCount == 1)
			{
				return await RunSingleValidationAsync(arguments, stopwatch);
			}

			return await RunParallelValidationsAsync(arguments, stopwatch);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Validation error: {ex.Message}");
			return 1;
		}
	}

	private async Task<int> RunSingleValidationAsync(CommandLineArguments arguments, Stopwatch stopwatch)
	{
		var persistedResultPath = BuildPersistedResultPath(arguments.JsonFilePath);
		try
		{
			var result = await _validator.ValidateAsync(arguments.JsonFilePath, arguments.SchemaFilePath);
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
		finally
		{
			DeleteIfExists(persistedResultPath);
			Console.WriteLine($"Cleaned serialized result: {persistedResultPath}");
		}
	}

	private async Task<int> RunParallelValidationsAsync(CommandLineArguments arguments, Stopwatch stopwatch)
	{
		// Load schema once to avoid concurrent registration errors
		var schema = await _validator.LoadSchemaAsync(arguments.SchemaFilePath);

		var tasks = new List<Task<(int index, EvaluationResults result, string path)>>();
		(EvaluationResults result, string path)[] completedResults = Array.Empty<(EvaluationResults result, string path)>();

		try
		{
			for (int i = 0; i < arguments.ParallelCount; i++)
			{
				var index = i;
				tasks.Add(ValidateAndPersistAsync(index, arguments, schema));
			}

			var results = await Task.WhenAll(tasks);
			completedResults = results.Select(r => (r.result, r.path)).ToArray();
			stopwatch.Stop();

			Console.WriteLine($"\n=== Parallel Validation Summary (Run count: {arguments.ParallelCount}) ===");
			Console.WriteLine($"Elapsed time (all validations to persistence): {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

			var allValid = true;
			var firstResult = results[0].result;

			foreach (var (index, result, path) in results)
			{
				var status = result.IsValid ? "✓ PASS" : "✗ FAIL";
				Console.WriteLine($"  Run {index + 1}: {status} - {path}");
				if (!result.IsValid)
				{
					allValid = false;
				}
			}

			if (!allValid)
			{
				Console.WriteLine("\nValidation failed: At least one run did not match the schema.");
				_issuePrinter.Print(firstResult);
				return 1;
			}

			Console.WriteLine("\nValidation succeeded: All parallel runs passed.");
			return 0;
		}
		finally
		{
			foreach (var (_, path) in completedResults)
			{
				DeleteIfExists(path);
				Console.WriteLine($"Cleaned serialized result: {path}");
			}
		}
	}

	private async Task<(int index, EvaluationResults result, string path)> ValidateAndPersistAsync(int index, CommandLineArguments arguments, JsonSchema schema)
	{
		var result = await _validator.ValidateAsync(arguments.JsonFilePath, schema);
		var persistedResultPath = BuildPersistedResultPathForParallel(arguments.JsonFilePath, index);
		await PersistResultAsync(result, arguments, persistedResultPath);
		return (index, result, persistedResultPath);
	}

	private static string BuildPersistedResultPath(string jsonFilePath)
	{
		var directory = Path.GetDirectoryName(jsonFilePath) ?? Environment.CurrentDirectory;
		var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
		return Path.Combine(directory, $"{fileName}.validation-result.json");
	}

	private static string BuildPersistedResultPathForParallel(string jsonFilePath, int runIndex)
	{
		var directory = Path.GetDirectoryName(jsonFilePath) ?? Environment.CurrentDirectory;
		var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
		return Path.Combine(directory, $"{fileName}.validation-result.run-{runIndex + 1:D3}.json");
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

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
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
