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
		if (args.Length != 2)
		{
			var usage = new List<string>
			{
				"Usage:",
				"  ElectricTruckJsonValidator <json-file-path> <json-schema-file-path>",
				string.Empty,
				"Example:",
				"  ElectricTruckJsonValidator samples/electric-truck-sample.json samples/electric-truck-schema.json"
			};

			return CommandLineArguments.Invalid(usage);
		}

		return CommandLineArguments.Valid(args[0], args[1]);
	}
}
