namespace ElectricTruckJsonValidator.Tests;

public class ElectricTruckPolymorphicDeserializerTests
{
    [Fact]
    public async Task DeserializeAndSummarizeAsync_WithKnownTaskTypes_ReturnsTypeCounts()
    {
        const string payload = """
        {
          "fleetId": "FLEET-1",
          "maintenance": {
            "tasks": [
              {
                "taskType": "BRAKE_CHECK",
                "taskCode": "BRAKE_CHECK",
                "priority": 2,
                "completed": true,
                "notes": "ok",
                "axleNumber": 1,
                "padThicknessMm": 9.1
              },
              {
                "taskType": "COOLANT_LOOP_INSPECTION",
                "taskCode": "COOLANT_LOOP_INSPECTION",
                "priority": 1,
                "completed": false,
                "notes": "inspect",
                "coolantLoop": "primary",
                "leakDetected": false
              }
            ]
          }
        }
        """;

        var path = CreateTempFile(payload);
        try
        {
            var sut = new ElectricTruckPolymorphicDeserializer();

            var result = await sut.DeserializeAndSummarizeAsync(path);

            Assert.True(result.IsSuccess);
            Assert.Contains("BrakeCheckTask=1", result.SummaryOrError);
            Assert.Contains("CoolantLoopInspectionTask=1", result.SummaryOrError);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DeserializeAndSummarizeAsync_WithInvalidJson_ReturnsError()
    {
        const string payload = "{ invalid-json }";

        var path = CreateTempFile(payload);
        try
        {
            var sut = new ElectricTruckPolymorphicDeserializer();

            var result = await sut.DeserializeAndSummarizeAsync(path);

            Assert.False(result.IsSuccess);
            Assert.Contains("Polymorphic deserialization error", result.SummaryOrError);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ElectricTruckPolymorphicDeserializerTests-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
