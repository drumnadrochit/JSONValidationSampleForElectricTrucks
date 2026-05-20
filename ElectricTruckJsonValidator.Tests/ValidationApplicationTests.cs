using Json.Schema;

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

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(0, exitCode);
			Assert.Contains("Validation succeeded", output);
		}
		finally
		{
			File.Delete(schemaFilePath);
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

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);
			var output = consoleCapture.GetOutput();

			Assert.Equal(1, exitCode);
			Assert.Contains("Validation failed", output);
			Assert.Contains("- ", output);
		}
		finally
		{
			File.Delete(schemaFilePath);
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WhenValidationSucceeds_DoesNotPrintIssues()
	{
		var jsonFilePath = CreateTempFile("{}");
		var schemaFilePath = CreateTempFile("{}");

		try
		{
			var issuePrinter = new FakeIssuePrinter();
			var app = new ValidationApplication(new FakeValidator(CreateValidationResult("{}", "{}")), issuePrinter);
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);

			Assert.Equal(0, exitCode);
			Assert.False(issuePrinter.WasCalled);
		}
		finally
		{
			File.Delete(schemaFilePath);
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task RunAsync_WhenValidationFails_InvokesIssuePrinter()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}";
		const string json = "{\"fleetId\":123}";
		var jsonFilePath = CreateTempFile("{}");
		var schemaFilePath = CreateTempFile("{}");

		try
		{
			var issuePrinter = new FakeIssuePrinter();
			var app = new ValidationApplication(new FakeValidator(CreateValidationResult(schema, json)), issuePrinter);
			var args = CommandLineArguments.Valid(jsonFilePath, schemaFilePath);

			using var consoleCapture = new ConsoleOutputCapture();
			var exitCode = await app.RunAsync(args);

			Assert.Equal(1, exitCode);
			Assert.True(issuePrinter.WasCalled);
		}
		finally
		{
			File.Delete(schemaFilePath);
			File.Delete(jsonFilePath);
		}
	}

	private static ValidationApplication CreateApplication()
	{
		return new ValidationApplication(new JsonSchemaValidator(), new EvaluationIssuePrinter());
	}

	private static EvaluationResults CreateValidationResult(string schemaText, string jsonText)
	{
		using var jsonDocument = System.Text.Json.JsonDocument.Parse(jsonText);
		var schema = JsonSchema.FromText(schemaText);
		return schema.Evaluate(jsonDocument.RootElement.Clone(), new EvaluationOptions
		{
			OutputFormat = OutputFormat.List
		});
	}

	private static string CreateTempFile(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ElectricTruckJsonValidatorTests-{Guid.NewGuid():N}.json");
		File.WriteAllText(path, content);
		return path;
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

	private sealed class FakeValidator(EvaluationResults result) : IJsonSchemaValidator
	{
		public Task<EvaluationResults> ValidateAsync(string jsonFilePath, string schemaFilePath)
		{
			return Task.FromResult(result);
		}
	}

	private sealed class FakeIssuePrinter : IEvaluationIssuePrinter
	{
		public bool WasCalled { get; private set; }

		public void Print(EvaluationResults results)
		{
			WasCalled = true;
		}
	}
}
