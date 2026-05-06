using Json.Schema;

namespace ElectricTruckJsonValidator;

/// <summary>
/// Prints recursive schema evaluation errors in a readable list format.
/// </summary>
public sealed class EvaluationIssuePrinter
{
	/// <summary>
	/// Writes all errors from the current evaluation node and its descendants.
	/// </summary>
	/// <param name="results">The root or child evaluation node to inspect.</param>
	public void Print(EvaluationResults results)
	{
		if (results.Errors is { Count: > 0 })
		{
			foreach (var error in results.Errors)
			{
				Console.WriteLine($"- {error.Key}: {error.Value}");
			}
		}

		if (results.Details is null || results.Details.Count == 0)
		{
			return;
		}

		// Json Schema results are hierarchical; recurse to surface nested failures.
		foreach (var child in results.Details)
		{
			Print(child);
		}
	}
}
