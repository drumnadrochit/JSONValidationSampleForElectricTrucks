using System.Text.Json;

namespace ElectricTruckJsonValidator;

public sealed class ElectricTruckPolymorphicDeserializer
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public async Task<(bool IsSuccess, string SummaryOrError)> DeserializeAndSummarizeAsync(string jsonFilePath)
	{
		var jsonText = await File.ReadAllTextAsync(jsonFilePath);

		ElectricTruckDocument? document;
		try
		{
			document = JsonSerializer.Deserialize<ElectricTruckDocument>(jsonText, SerializerOptions);
		}
		catch (Exception ex)
		{
			return (false, $"Polymorphic deserialization error: {ex.Message}");
		}

		if (document is null)
		{
			return (false, "Polymorphic deserialization error: payload could not be parsed.");
		}

		var tasks = document.Maintenance?.Tasks ?? [];
		if (tasks.Count == 0)
		{
			return (true, "Polymorphic deserialization completed: no maintenance tasks found.");
		}

		var grouped = tasks
			.GroupBy(task => task.GetType().Name)
			.OrderBy(group => group.Key)
			.Select(group => $"{group.Key}={group.Count()}");

		return (true, $"Polymorphic deserialization completed: {string.Join(", ", grouped)}.");
	}
}