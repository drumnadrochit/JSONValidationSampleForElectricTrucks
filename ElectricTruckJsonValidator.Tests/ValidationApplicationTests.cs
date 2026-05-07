namespace ElectricTruckJsonValidator.Tests;

public class ValidationApplicationTests
{
	[Fact]
	public async Task RunAsync_WithMissingJsonFile_ReturnsFailureAndPrintsMessage()
	{
		var app = CreateApplication();
		var args = CommandLineArguments.Valid("missing-input.json", "missing-schema.json");

		using var consoleCapture = new ConsoleOutputCapture();
		var exitCode = await app.RunAsync(args);
		var output = consoleCapture.GetOutput();

		Assert.Equal(1, exitCode);
		Assert.Contains("JSON file was not found", output);
	}

	[Fact]
	public async Task RunAsync_WithMissingSchemaFile_ReturnsFailureAndPrintsMessage()
	{
		var jsonFilePath = CreateTempFile("{\"fleetId\":\"fleet-1\"}");
		try
		{
			var app = CreateApplication();
			var args = CommandLineArguments.Valid(jsonFilePath, "missing-schema.json");

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(1, exitCode);
			Assert.Contains("JSON schema file was not found", output);
		}
		finally
		{
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WithValidJsonAgainstSchema_ReturnsSuccess()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}";
		const string json = "{\"fleetId\":\"fleet-1\"}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var app = CreateApplication();
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath);
			var persistedPath = BuildPersistedResultPath(jsonFilePath);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(0, exitCode);
			Assert.Contains("Validation succeeded", output);
			Assert.Contains("Elapsed time (validation to persistence)", output);
			Assert.Contains("Cleaned serialized result", output);
			Assert.False(File.Exists(persistedPath));
		}
		finally
		{
			File.Delete(schemaFilePath);
			DeletePersistedFileIfExists(jsonFilePath);
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WithInvalidJsonAgainstSchema_ReturnsFailureAndPrintsIssues()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}";
		const string json = "{\"fleetId\":123}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var app = CreateApplication();
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath);
			var persistedPath = BuildPersistedResultPath(jsonFilePath);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(1, exitCode);
			Assert.Contains("Validation failed", output);
			Assert.Contains("- ", output);
			Assert.Contains("Elapsed time (validation to persistence)", output);
			Assert.Contains("Cleaned serialized result", output);
			Assert.False(File.Exists(persistedPath));
		}
		finally
		{
			File.Delete(schemaFilePath);
			DeletePersistedFileIfExists(jsonFilePath);
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WithParallelCountOfThree_RunsValidationThreeTimesInParallel()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}";
		const string json = "{\"fleetId\":\"fleet-1\"}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var app = CreateApplication();
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath, parallelCount: 3);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(0, exitCode);
			Assert.Contains("Parallel Validation Summary", output);
			Assert.Contains("Run count: 3", output);
			Assert.Contains("Elapsed time (all validations to persistence)", output);
			Assert.Contains("Run 1:", output);
			Assert.Contains("Run 2:", output);
			Assert.Contains("Run 3:", output);
			Assert.Contains("PASS", output);

			// Verify all three result files were cleaned up at the end
			Assert.Contains("Cleaned serialized result", output);
			Assert.False(File.Exists(BuildPersistedResultPathForParallel(jsonFilePath, 0)));
			Assert.False(File.Exists(BuildPersistedResultPathForParallel(jsonFilePath, 1)));
			Assert.False(File.Exists(BuildPersistedResultPathForParallel(jsonFilePath, 2)));
		}
		finally
		{
			File.Delete(schemaFilePath);
			for (int i = 0; i < 3; i++)
			{
				var path = BuildPersistedResultPathForParallel(jsonFilePath, i);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WithParallelCountAndInvalidJson_ReturnsFailureForAllRuns()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}";
		const string json = "{\"fleetId\":123}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var app = CreateApplication();
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath, parallelCount: 2);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(1, exitCode);
			Assert.Contains("Parallel Validation Summary", output);
			Assert.Contains("At least one run did not match", output);
			Assert.Contains("Run 1:", output);
			Assert.Contains("Run 2:", output);
			Assert.Contains("FAIL", output);
			Assert.Contains("Cleaned serialized result", output);
			Assert.False(File.Exists(BuildPersistedResultPathForParallel(jsonFilePath, 0)));
			Assert.False(File.Exists(BuildPersistedResultPathForParallel(jsonFilePath, 1)));
		}
		finally
		{
			File.Delete(schemaFilePath);
			for (int i = 0; i < 2; i++)
			{
				var path = BuildPersistedResultPathForParallel(jsonFilePath, i);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			File.Delete(jsonFilePath);
		}
	}

	private static ValidationApplication CreateApplication()
	{
		return new ValidationApplication(new JsonSchemaValidator(), new EvaluationIssuePrinter());
	}

	private static string CreateTempFile(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ElectricTruckJsonValidatorTests-{Guid.NewGuid():N}.json");
		File.WriteAllText(path, content);
		return path;
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

	private static void DeletePersistedFileIfExists(string jsonFilePath)
	{
		var path = BuildPersistedResultPath(jsonFilePath);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	private sealed class ConsoleOutputCapture : IDisposable
	{
		private readonly StringWriter _writer = new();
		private readonly TextWriter _original = Console.Out;

		public ConsoleOutputCapture()
		{
			Console.SetOut(_writer);
		}

		public string GetOutput()
		{
			_writer.Flush();
			return _writer.ToString();
		}

		public void Dispose()
		{
			Console.SetOut(_original);
			_writer.Dispose();
		}
	}
}
