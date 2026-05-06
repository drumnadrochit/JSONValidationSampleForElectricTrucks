namespace ElectricTruckJsonValidator.Tests;

public class CommandLineArgumentParserTests
{
    [Fact]
    public void Parse_WithIncorrectArgumentCount_ReturnsInvalidResultWithUsageMessages()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(Array.Empty<string>());

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Messages);
        Assert.Contains(result.Messages, m => m.Contains("Usage:"));
    }

    [Fact]
    public void Parse_WithTwoArguments_ReturnsValidResult()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(new[] { "input.json", "schema.json" });

        Assert.True(result.IsValid);
        Assert.Equal("input.json", result.JsonFilePath);
        Assert.Equal("schema.json", result.SchemaFilePath);
        Assert.Empty(result.Messages);
    }
}
