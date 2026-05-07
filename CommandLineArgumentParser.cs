namespace ElectricTruckJsonValidator;

/// <summary>
/// Parses and validates command-line arguments for the validator.
/// </summary>
public sealed class CommandLineArgumentParser
{
	/// <summary>
	/// Parses raw command-line tokens into a strongly typed argument object.
	/// </summary>
	/// <param name="args">Raw process arguments.</param>
	/// <returns>A valid or invalid argument result with guidance messages.</returns>
	public CommandLineArguments Parse(string[] args)
	{
		if (args.Length < 2 || args.Length > 3)
		{
			var usage = new List<string>
			{
				"Usage:",
				"  ElectricTruckJsonValidator <json-file-path> <json-schema-file-path> [parallel-count]",
				string.Empty,
				"Arguments:",
				"  json-file-path         Path to the JSON instance file",
				"  json-schema-file-path  Path to the JSON schema file",
				"  parallel-count         (Optional) Number of parallel validation runs (default: 1)",
				string.Empty,
				"Example:",
				"  ElectricTruckJsonValidator samples/electric-truck-sample.json samples/electric-truck-schema.json",
				"  ElectricTruckJsonValidator samples/electric-truck-sample.json samples/electric-truck-schema.json 5"
			};

			return CommandLineArguments.Invalid(usage);
		}

		int parallelCount = 1;
		if (args.Length == 3)
		{
			if (!int.TryParse(args[2], out var parsed) || parsed < 1)
			{
				var messages = new List<string>
				{
					"Error: parallel-count must be a positive integer.",
					string.Empty,
					"Received: " + args[2]
				};
				return CommandLineArguments.Invalid(messages);
			}
			parallelCount = parsed;
		}

		return CommandLineArguments.Valid(args[0], args[1], parallelCount);
	}
}
