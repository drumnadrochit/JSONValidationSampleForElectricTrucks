using Json.Schema;

namespace ElectricTruckJsonValidator;

/// <summary>
/// Writes validation issues for an evaluation result.
/// </summary>
public interface IEvaluationIssuePrinter
{
	/// <summary>
	/// Writes all relevant issues from the supplied evaluation results.
	/// </summary>
	/// <param name="results">The root or child evaluation node to inspect.</param>
	void Print(EvaluationResults results);
}