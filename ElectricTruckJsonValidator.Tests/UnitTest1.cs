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
    public void Parse_WithTwoArguments_ReturnsValidResultWithDefaultParallelCount()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(new[] { "input.json", "schema.json" });

        Assert.True(result.IsValid);
        Assert.Equal("input.json", result.JsonFilePath);
        Assert.Equal("schema.json", result.SchemaFilePath);
        Assert.Equal(1, result.ParallelCount);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Parse_WithThreeArgumentsAndValidParallelCount_ReturnsValidResult()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(new[] { "input.json", "schema.json", "5" });

        Assert.True(result.IsValid);
        Assert.Equal("input.json", result.JsonFilePath);
        Assert.Equal("schema.json", result.SchemaFilePath);
        Assert.Equal(5, result.ParallelCount);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Parse_WithInvalidParallelCount_ReturnsInvalidResult()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(new[] { "input.json", "schema.json", "abc" });

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Messages);
        Assert.Contains(result.Messages, m => m.Contains("positive integer"));
    }

    [Fact]
    public void Parse_WithZeroParallelCount_ReturnsInvalidResult()
    {
        var parser = new CommandLineArgumentParser();

        var result = parser.Parse(new[] { "input.json", "schema.json", "0" });

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Messages);
        Assert.Contains(result.Messages, m => m.Contains("positive integer"));
    }
}
