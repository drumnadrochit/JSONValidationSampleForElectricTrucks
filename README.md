# ElectricTruckJsonValidator

Command-line app to validate a JSON document against a JSON Schema.

This solution now also includes a containerized microservice architecture with:

- one control node responsible for orchestration and load distribution
- n validation nodes responsible for executing schema validation

This project is intentionally small and focused. It provides a clean example of:

- Parsing command-line input
- Validating input and schema files
- Evaluating JSON with JsonSchema.Net
- Returning process-friendly exit codes for scripts/CI
- Unit-testing console application logic
- Running validation as distributed HTTP microservices
- Deploying the services as containers with Docker Compose

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

## Microservice architecture

The solution contains three application types:

- `ElectricTruckJsonValidator`: the original CLI validator
- `ControlNode.Api`: the control node that accepts distributed validation requests and fans them out
- `ValidationNode.Api`: the worker node that performs JSON Schema validation

### Control node responsibilities

- Accept a validation payload and desired execution count
- Select validation nodes using round-robin distribution
- Dispatch validations concurrently
- Aggregate node responses into one distributed result

### Validation node responsibilities

- Accept JSON and schema text over HTTP
- Evaluate the JSON document against the schema
- Return validity, issues, duration, and node identity

## Distributed API contract

### Control node endpoint

`POST /distributed-validation`

Example request:

```json
{
   "jsonDocument": "{\"fleetId\":\"fleet-1\"}",
   "jsonSchema": "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\":[\"fleetId\"],\"properties\":{\"fleetId\":{\"type\":\"string\"}}}",
   "validationCount": 5,
   "correlationId": "request-001"
}
```

### Validation node endpoint

`POST /validate`

This endpoint is intended to be called by the control node, not directly by external clients.

## Containerized deployment

### Build and run the full topology

```bash
docker compose up --build
```

This starts:

- `control-node` on port `8080`
- `validation-node-1`
- `validation-node-2`
- `validation-node-3`

The control node is configured through environment variables to discover the validation nodes. You can scale the number of validation nodes by:

1. adding more `validation-node-*` services in `docker-compose.yml`
2. adding matching `ValidationNodes__Urls__*` entries for the control node

### Example distributed request

```bash
curl -X POST http://localhost:8080/distributed-validation \
   -H "Content-Type: application/json" \
   -d '{
      "jsonDocument": "{""fleetId"":""fleet-1""}",
      "jsonSchema": "{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""type"":""object"",""required"":[""fleetId""],""properties"":{""fleetId"":{""type"":""string""}}}",
      "validationCount": 3,
      "correlationId": "demo-run"
   }'
```

## Deployment assets

The repository now includes:

- `ControlNode.Api/Dockerfile`
- `ValidationNode.Api/Dockerfile`
- `docker-compose.yml`
- `.dockerignore`

## Expected output

- Success: `Validation succeeded: JSON is valid against the schema.`
- Failure: `Validation failed: JSON does not match the schema.` and a list of issues.
- Persistence: `Validation result persisted to: <path>`
- Timing: `Elapsed time (validation to persistence): <milliseconds> ms`
- Cleanup: `Cleaned serialized result: <path>`

During execution, the app serializes a result file next to the input JSON:

- Single run: `<input-file-name>.validation-result.json`
- Parallel runs: `<input-file-name>.validation-result.run-001.json`, `run-002.json`, etc.

The file includes validation status, input paths, persistence timestamp, and collected issues.

At the end of the run, the serialized result file(s) are deleted automatically.

### Parallel Validation

When the optional `parallel-count` parameter is provided (value ≥ 1), the validator runs that many validation-and-serialization cycles concurrently:

```text
ElectricTruckJsonValidator input.json schema.json 10
```

This runs the validation workflow 10 times in parallel against the same schema and input file. Each run:
- Validates the JSON against the schema
- Serializes the result to a timestamped file (`run-001.json`, `run-002.json`, etc.)

The elapsed time reported spans from the start of the first validation to the completion of the last persisted file write. A summary table shows the result of each parallel run.

## Command-line contract

The app expects two or three arguments:

1. Path to the JSON instance file
2. Path to the JSON schema file
3. (Optional) Number of parallel validation runs (default: 1)

Usage message:

```text
ElectricTruckJsonValidator <json-file-path> <json-schema-file-path> [parallel-count]
```

Examples:

```text
ElectricTruckJsonValidator samples/electric-truck-sample.json samples/electric-truck-schema.json
ElectricTruckJsonValidator samples/electric-truck-sample.json samples/electric-truck-schema.json 5
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

Microservice files:

- [ControlNode.Api/Program.cs](ControlNode.Api/Program.cs): Control plane API and round-robin distribution
- [ValidationNode.Api/Program.cs](ValidationNode.Api/Program.cs): Worker API and JSON schema execution
- [Validation.Contracts/ValidationModels.cs](Validation.Contracts/ValidationModels.cs): Shared API contracts

Sample assets:

- [samples/electric-truck-sample.json](samples/electric-truck-sample.json): Example payload
- [samples/electric-truck-schema.json](samples/electric-truck-schema.json): Example schema

Test project:

- [ElectricTruckJsonValidator.Tests/ElectricTruckJsonValidator.Tests.csproj](ElectricTruckJsonValidator.Tests/ElectricTruckJsonValidator.Tests.csproj)
- [ElectricTruckJsonValidator.Tests/UnitTest1.cs](ElectricTruckJsonValidator.Tests/UnitTest1.cs): Parser tests
- [ElectricTruckJsonValidator.Tests/ValidationApplicationTests.cs](ElectricTruckJsonValidator.Tests/ValidationApplicationTests.cs): Workflow and output tests
- [ElectricTruckJsonValidator.Tests/JsonSchemaValidatorTests.cs](ElectricTruckJsonValidator.Tests/JsonSchemaValidatorTests.cs): Validator behavior tests

## Validation flow

**Single-run mode (default, parallel-count=1):**
1. Parse and validate CLI arguments
2. Verify the JSON file exists
3. Verify the schema file exists
4. Read both files
5. Parse JSON and evaluate against schema with list-formatted output
6. Serialize result to disk
7. Print success/failure and any collected issues
8. Return exit code for shell/automation integration

**Parallel-run mode (parallel-count > 1):**
1. Parse and validate CLI arguments
2. Verify the JSON file exists
3. Verify the schema file exists
4. Launch `parallel-count` concurrent validation-and-serialization tasks:
   - Each task reads files, validates, and writes a timestamped result
5. Wait for all tasks to complete
6. Print a summary table with per-run results and total elapsed time
7. Return exit code (0 only if all parallel runs passed)

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
