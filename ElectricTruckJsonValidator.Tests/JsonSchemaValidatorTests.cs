namespace ElectricTruckJsonValidator.Tests;

public class JsonSchemaValidatorTests
{
	[Fact]
	public async Task ValidateAsync_WhenInstanceMatchesSchema_ReturnsValidResult()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"truckId\"],\"properties\":{\"truckId\":{\"type\":\"string\"}}}";
		const string json = "{\"truckId\":\"TRK-1\"}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var validator = new JsonSchemaValidator();

			var result = await validator.ValidateAsync(jsonFilePath, schemaFilePath);

			Assert.True(result.IsValid);
		}
		finally
		{
			File.Delete(schemaFilePath);
			File.Delete(jsonFilePath);
		}
	}

	[Fact]
	public async Task ValidateAsync_WhenInstanceDoesNotMatchSchema_ReturnsInvalidResult()
	{
		const string schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"truckId\"],\"properties\":{\"truckId\":{\"type\":\"string\"}}}";
		const string json = "{\"truckId\":42}";

		var schemaFilePath = CreateTempFile(schema);
		var jsonFilePath = CreateTempFile(json);

		try
		{
			var validator = new JsonSchemaValidator();

			var result = await validator.ValidateAsync(jsonFilePath, schemaFilePath);

			Assert.False(result.IsValid);
		}
		finally
		{
			File.Delete(schemaFilePath);
			File.Delete(jsonFilePath);
		}
	}

	private static string CreateTempFile(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ElectricTruckJsonValidatorTests-{Guid.NewGuid():N}.json");
		File.WriteAllText(path, content);
		return path;
	}
}
