# ElectricTruckJsonValidator

Command-line app to validate a JSON document against a JSON Schema.

## Prerequisites

- .NET SDK 9 installed (or .NET 10 when available on your machine)

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run -- samples/electric-truck-sample.json samples/electric-truck-schema.json
```

## Expected output

- Success: `Validation succeeded: JSON is valid against the schema.`
- Failure: `Validation failed: JSON does not match the schema.` and a list of issues.

## Notes on .NET 10

This machine currently has .NET 8/9 SDKs only. To target .NET 10 once installed, update `TargetFramework` in `ElectricTruckJsonValidator.csproj` to `net10.0`.
