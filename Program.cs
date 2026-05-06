namespace ElectricTruckJsonValidator;

/// <summary>
/// Console entry point for validating an electric truck JSON document against a JSON schema.
/// </summary>
public static class Program
{
	/// <summary>
	/// Starts the validation flow and returns a process exit code.
	/// </summary>
	/// <param name="args">Expected arguments: JSON file path and schema file path.</param>
	/// <returns><c>0</c> when validation succeeds; otherwise <c>1</c>.</returns>
	public static async Task<int> Main(string[] args)
	{
		var parser = new CommandLineArgumentParser();
		var arguments = parser.Parse(args);

		if (!arguments.IsValid)
		{
			foreach (var message in arguments.Messages)
			{
				Console.WriteLine(message);
			}

			return 1;
		}

		var app = new ValidationApplication(new JsonSchemaValidator(), new EvaluationIssuePrinter());
		return await app.RunAsync(arguments);
	}
}
