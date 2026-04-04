# DevMaid Development Guidelines

Auto-generated from codebase. Last updated: 2026-04-04

## Active Technologies

- C# 13 / .NET 10 (`net10.0`)
- System.CommandLine 2.0.5 — CLI argument parsing and subcommand routing
- Npgsql 10.0.2 — PostgreSQL ADO.NET driver
- CsvHelper 33.1.0 — CSV export for query results
- Microsoft.Extensions.Hosting 10.0.5 — Generic host + DI container
- Microsoft.Extensions.Configuration — JSON / env-var config pipeline
- Microsoft.Extensions.Logging — Structured logging (wrapped by custom ILogger)
- Polly 8.5.2 — Resilience and retry pipelines
- MSTest 4.1.0 + Moq 4.20.72 — Unit testing and mocking
- Nerdbank.GitVersioning 3.9.50 — Semantic versioning from git history

## Project Structure

```text
DevMaid/
├── DevMaid.Core/                  ← Business logic, interfaces, models (class library)
│   ├── Interfaces/                ← IConfigurationService, IDatabaseService, IFileService,
│   │                                 IProcessExecutor, IWingetService
│   ├── Models/                    ← OperationResult<T>, DatabaseConnectionConfig,
│   │                                 OperationProgress, WingetOperationOptions, etc.
│   ├── Services/                  ← Concrete implementations of all interfaces
│   │   └── ServiceCollectionExtensions.cs  ← AddDevMaidServices() DI registration
│   ├── Logging/                   ← Custom ILogger + MicrosoftExtensionsLoggerAdapter
│   ├── HealthChecks/              ← PostgresBinaryHealthCheck, ConfigurationHealthCheck
│   └── Resilience/                ← ResiliencePolicies (Polly retry pipelines)
│
├── DevMaid.CLI/                   ← CLI executable (depends on DevMaid.Core)
│   ├── Program.cs                 ← Host builder, DI setup, RootCommand registration
│   ├── Commands/                  ← One static class per command, Build() factory method
│   │   ├── TableParserCommand.cs
│   │   ├── FileCommand.cs
│   │   ├── ClaudeCodeCommand.cs
│   │   ├── OpenCodeCommand.cs
│   │   ├── WingetCommand.cs
│   │   ├── DatabaseCommand.cs
│   │   ├── QueryCommand.cs
│   │   ├── CleanCommand.cs
│   │   └── WindowsFeaturesCommand.cs
│   ├── CommandOptions/            ← Strongly-typed options DTOs per command
│   ├── Services/                  ← Static facade wrappers (ConfigurationService, Logger, etc.)
│   └── SecurityUtils.cs           ← Input validation (path traversal, PostgreSQL identifiers)
│
├── DevMaid.Tests/                 ← MSTest project (references DevMaid.CLI)
│   └── Commands/                  ← One test class per command
│
├── DevMaid.CodeAnalysis/          ← Standalone Roslyn analysis utility
│
├── specs/                         ← Feature specs (speckit SDD workflow)
│   ├── README.md                  ← Master index of all specs
│   └── <NNN>-<slug>/              ← spec.md, plan.md, tasks.md per feature
│
├── docs/
│   ├── pt-BR/                     ← Primary documentation (Portuguese, source of truth)
│   └── en/                        ← Secondary documentation (English)
│
└── .specify/
    ├── memory/constitution.md     ← Project constitution (governing principles)
    └── templates/                 ← speckit templates (spec, plan, tasks, agent)
```

## Commands

```bash
# Build and run
dotnet restore
dotnet build
dotnet build -c Release
dotnet run -- --help
dotnet run -- <command> [args]

# Test
dotnet test
dotnet test --filter "ClassName=DatabaseCommandTests"

# Format (runs dotnet format on all projects)
./format.ps1       # PowerShell 7, parallel
./format.bat       # Batch, sequential

# Publish
./publish.bat nuget   # Creates .nupkg in bin/Release/net10.0/
./publish.bat exe     # Creates win-x64 single-file exe in bin/publish/exe/
./publish.bat all     # Both

# Install as global tool (from source)
dotnet tool install --add-source bin/Release/net10.0/ devmaid --global

# CLI usage
devmaid --help
devmaid database backup <dbname> [--host] [--port] [--username] [--password] [--output]
devmaid database restore <dbname> --input <file> [connection options]
devmaid database backup --all [connection options]
devmaid table-parser -d <db> -t <table> -H <host> -u <user>
devmaid file combine -i "<glob>" -o <output>
devmaid query run -f <sql-file> -d <db> [--all] [--servers]
devmaid clean [path]
devmaid winget backup -o <dir>
devmaid winget restore -i <json-file>
devmaid claude install
devmaid claude settings mcp-database
devmaid claude settings win-env
devmaid opencode settings mcp-database
devmaid windowsfeatures export -o <file>
devmaid windowsfeatures import -i <file>
devmaid windowsfeatures list
```

## Code Style

- **Language**: C# 13, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Naming**: PascalCase for types/methods/properties; camelCase for locals and parameters
- **Command classes**: Always `static`, expose `static Build(): Command` factory — never inherit
- **Service classes**: Always implement a `IXxxService` interface in `DevMaid.Core/Interfaces/`; concrete implementation in `DevMaid.Core/Services/`
- **No business logic in commands**: Commands parse input into a typed Options DTO, then call the service method. Never put logic inside the command handler lambda.
- **Result pattern**: Return `OperationResult` / `OperationResult<T>` records — use `SuccessResult(...)` / `FailureResult(...)` factory methods
- **External processes**: Always `UseShellExecute = false`, capture stdout/stderr via redirect — never shell out to `cmd.exe` or `powershell.exe`
- **Test naming**: `<MethodName>_<StateUnderTest>_<ExpectedBehavior>` (e.g., `BackupAsync_ComOpcoesValidas_DeveCriarArquivoDump`)
- **XML doc comments**: Required on all public members (enforced by `GenerateDocumentationFile true`)
- **Configuration**: Read from `%LocalAppData%\DevMaid\appsettings.json`; never hard-code connection strings; use `SecurityUtils.IsValidPath()` and `SecurityUtils.IsValidPostgreSQLIdentifier()` before using any user-supplied input
- **Async**: All service methods that touch I/O or external processes must be `async Task<T>` and accept `CancellationToken`
- **Progress reporting**: Use `IProgress<OperationProgress>` for operations expected to take > 2s
- **Logging**: Use `ILogger` from `DevMaid.Core/Logging/ILogger.cs` — not `Microsoft.Extensions.Logging.ILogger` directly in Core

## Architecture Constraints

- Dependencies flow **downward only**: `Tests` → `CLI` → `Core`. Never reference a higher layer from a lower one.
- New NuGet packages require justification in the feature spec's "Decisões Técnicas" section.
- New features may introduce at most **one new project** — additional projects require explicit approval.
- All inputs validated before use: paths via `SecurityUtils.IsValidPath()`, PostgreSQL identifiers via `SecurityUtils.IsValidPostgreSQLIdentifier()`.
- Exit codes: `0` success, `1` general error, `2` invalid args, `3` external dependency not found, `130` user cancellation.

## Recent Changes

- 009-windows-features-manager: Added `WindowsFeaturesCommand` — dism.exe wrapper for export/import/list of Windows optional features
- 008-project-cleaner: Added `CleanCommand` — recursive `bin/` and `obj/` directory deletion
- 007-sql-query-csv-export: Added `QueryCommand` — SQL file execution with CSV export, multi-server support

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
