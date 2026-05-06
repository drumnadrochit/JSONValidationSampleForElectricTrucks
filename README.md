# ElectricTruckJsonValidator

Command-line app to validate a JSON document against a JSON Schema.

This project is intentionally small and focused. It provides a clean example of:

- Parsing command-line input
- Validating input and schema files
- Evaluating JSON with JsonSchema.Net
- Returning process-friendly exit codes for scripts/CI
- Unit-testing console application logic

## What it validates

The validator compares:

- A JSON instance file (for example, truck telemetry and operations data)
- A JSON Schema file that defines the contract for that data

If the instance conforms to the schema, validation succeeds. If not, the app prints a recursive list of evaluation errors.

## Prerequisites

- .NET SDK 9 installed (or .NET 10 when available on your machine)
- PowerShell, cmd, or Bash terminal

## Quick start

### Build

```bash
dotnet build JasonValidationForElectricTrucks.sln
```

### Run with sample files

```bash
dotnet run -- samples/electric-truck-sample.json samples/electric-truck-schema.json
```

### Run tests

```bash
dotnet test JasonValidationForElectricTrucks.sln
```

## Expected output

- Success: `Validation succeeded: JSON is valid against the schema.`
- Failure: `Validation failed: JSON does not match the schema.` and a list of issues.
- Persistence: `Validation result persisted to: <path>`
- Timing: `Elapsed time (validation to persistence): <milliseconds> ms`

After each validation run, the app also serializes a result file next to the input JSON:

- `<input-file-name>.validation-result.json`

The file includes validation status, input paths, persistence timestamp, and collected issues.

## Command-line contract

The app expects exactly two arguments:

1. Path to the JSON instance file
2. Path to the JSON schema file

Usage message:

```text
ElectricTruckJsonValidator <json-file-path> <json-schema-file-path>
```

### Exit codes

- 0: Validation succeeded
- 1: Invalid arguments, missing files, validation failures, or runtime errors

## Project structure

Main application files:

- [Program.cs](Program.cs): Entry point, argument parsing handoff, and application execution
- [CommandLineArgumentParser.cs](CommandLineArgumentParser.cs): Validates and parses raw CLI arguments
- [CommandLineArguments.cs](CommandLineArguments.cs): Immutable argument model with validity/messages
- [ValidationApplication.cs](ValidationApplication.cs): End-to-end workflow orchestration
- [JsonSchemaValidator.cs](JsonSchemaValidator.cs): JsonSchema.Net integration and evaluation logic
- [EvaluationIssuePrinter.cs](EvaluationIssuePrinter.cs): Recursive issue printing for nested schema failures

Sample assets:

- [samples/electric-truck-sample.json](samples/electric-truck-sample.json): Example payload
- [samples/electric-truck-schema.json](samples/electric-truck-schema.json): Example schema

Test project:

- [ElectricTruckJsonValidator.Tests/ElectricTruckJsonValidator.Tests.csproj](ElectricTruckJsonValidator.Tests/ElectricTruckJsonValidator.Tests.csproj)
- [ElectricTruckJsonValidator.Tests/UnitTest1.cs](ElectricTruckJsonValidator.Tests/UnitTest1.cs): Parser tests
- [ElectricTruckJsonValidator.Tests/ValidationApplicationTests.cs](ElectricTruckJsonValidator.Tests/ValidationApplicationTests.cs): Workflow and output tests
- [ElectricTruckJsonValidator.Tests/JsonSchemaValidatorTests.cs](ElectricTruckJsonValidator.Tests/JsonSchemaValidatorTests.cs): Validator behavior tests

## Validation flow

1. Parse and validate CLI arguments
2. Verify the JSON file exists
3. Verify the schema file exists
4. Read both files
5. Parse JSON and evaluate against schema with list-formatted output
6. Print success/failure and any collected issues
7. Return exit code for shell/automation integration

## JSON schema engine

This app uses JsonSchema.Net (package JsonSchema.Net, currently 9.2.0) and evaluates with list output mode so failures can be presented as a readable issue list.

## Troubleshooting

### Build command says multiple project/solution files found

Use an explicit target:

```bash
dotnet build JasonValidationForElectricTrucks.sln
```

### App says input file was not found

- Confirm your current working directory
- Use absolute paths if needed
- Verify file name spelling and extension

### Schema validation fails unexpectedly

- Validate that required properties exist
- Check property types (string vs number, etc.)
- Ensure formats and min/max constraints match your data
- Inspect printed error paths to locate the failing field quickly

## CI usage example

Example script snippet for CI/CD pipelines:

```bash
dotnet run --project ElectricTruckJsonValidator.csproj -- data/input.json data/schema.json
```

Use the process exit code to fail a pipeline step when data does not match the schema.

## Notes on .NET 10

This machine currently has .NET 8/9 SDKs only. To target .NET 10 once installed, update `TargetFramework` in `ElectricTruckJsonValidator.csproj` to `net10.0`.
