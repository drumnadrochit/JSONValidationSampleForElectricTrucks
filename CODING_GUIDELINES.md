# ElectricTruckJsonValidator Coding Principles

This document adapts the attached coding principles to the current ElectricTruckJsonValidator codebase.
It is intended to keep future changes consistent with the existing CLI-focused architecture and tests.

## 1. Core Architectural Principles

1. Keep responsibilities separated by workflow stage.

- Entry point: bootstrap dependencies, parse arguments, and return exit codes.
- Parsing: validate raw CLI input and produce a strongly typed result.
- Application workflow: coordinate file existence checks, validation, and console messaging.
- Validation engine: own JSON Schema evaluation and integration with JsonSchema.Net.
- Output formatting: print nested validation issues without owning validation rules.

1. Keep dependency direction simple and explicit.

- `Program` should compose the application from concrete collaborators.
- `ValidationApplication` should orchestrate the workflow, not parse raw arguments or implement schema logic.
- `JsonSchemaValidator` should own schema evaluation details.
- `EvaluationIssuePrinter` should own recursive issue formatting only.

1. Favor the explicit patterns already present.

- Small immutable input/result models.
- Constructor-injected collaborators for testability.
- Clear process-friendly success and failure paths.

## 2. File-by-File Coding Rules

## Entry Point

- Keep `Program` thin.
- Limit `Program` to composition, invalid-argument output, and the final call into the application workflow.
- Do not move file I/O or schema evaluation into the entry point.

## Argument Parsing

- Keep `CommandLineArgumentParser` focused on raw argument count and usage guidance.
- Return structured results through `CommandLineArguments` instead of throwing for normal user mistakes.
- Keep usage text stable unless the command-line contract changes.

## Application Workflow

- Keep `ValidationApplication` focused on orchestration.
- Preserve the current order of checks: arguments, file existence, validation, issue printing, exit code.
- Return process-friendly exit codes instead of leaking exceptions to the shell.
- Catch runtime failures at the workflow boundary and emit a safe, concise console message.

## Validation Engine

- Keep JSON parsing and JsonSchema.Net interaction inside `JsonSchemaValidator`.
- Do not spread schema-library-specific behavior across `Program` or `ValidationApplication`.
- Keep validation APIs async end-to-end for file I/O.

## Output Formatting

- Keep recursive issue rendering inside `EvaluationIssuePrinter`.
- Prefer readable output over raw library object dumps.
- Preserve the existing bullet-style issue formatting because tests and CLI usage depend on it.

## 3. Error Handling and Validation Guidance

- Treat incorrect CLI usage, missing files, schema mismatches, and runtime failures as process failures with exit code `1`.
- Print actionable messages for user-correctable issues.
- Avoid swallowing exceptions silently.
- Avoid exposing unnecessary internal details beyond the current concise error message pattern.

## 4. Async, I/O, and Performance

- Keep file reading and validation async.
- Avoid unnecessary buffering or duplicate file reads.
- Keep the app single-purpose and synchronous in flow even when using async I/O.

## 5. Testing Guidance

- Keep tests close to behavior boundaries.
- Parser tests should verify argument validity and usage output.
- Application tests should verify exit codes and console output for success and failure paths.
- Validator tests should verify valid and invalid schema outcomes with minimal fixture data.
- When changing console output, update tests deliberately rather than allowing accidental contract drift.

## 6. Naming and Style Conventions

- Keep type names explicit and task-oriented, such as `CommandLineArgumentParser`, `ValidationApplication`, and `JsonSchemaValidator`.
- Prefer small methods with one responsibility.
- Keep public APIs nullable-safe and consistent with the existing C# style in the repo.
- Preserve the current output contract unless the user explicitly asks to change it.

## 7. Change Checklist

1. Keep the command-line contract explicit and documented.
2. Preserve or intentionally update exit code behavior.
3. Keep schema-library details isolated in the validator.
4. Keep issue formatting isolated in the printer.
5. Add or update focused tests for any changed behavior.
6. Verify the solution still builds and tests pass.

## 8. What to Avoid

- Fat `Program` logic.
- Console output scattered across multiple classes without a clear reason.
- JsonSchema.Net-specific logic outside the validator.
- Unstructured exceptions for normal user input errors.
- Broad refactors that change the CLI contract without corresponding tests and documentation updates.
