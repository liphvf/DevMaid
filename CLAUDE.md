# FurLab Development Guidelines

Auto-generated from codebase. Last updated: 2026-04-19

## Idioma

Todo output para o usuário deve ser em **português do Brasil (pt-BR)**.
Isso inclui explicações, resumos, mensagens de erro, perguntas de esclarecimento e qualquer comunicação direta com o usuário.
Exceção: não traduza nomes de skills, comandos do openspec (ex: `openspec-propose`, `opsx-apply`), identificadores de código, nomes de arquivos ou comandos de terminal.

Todas as **mensagens exibidas ao usuário pelo CLI** (output de comandos, prompts interativos, mensagens de erro e confirmações) devem ser escritas em **inglês**.

## Active Technologies

- C# 13 / .NET 10 (`net10.0`)
- Spectre.Console.Cli 0.55.0 — Interactive TUI prompts and CLI argument parsing
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
FurLab/
├── FurLab.Core/                  ← Business logic, interfaces, models (class library)
│   ├── Interfaces/                ← ICredentialService, IDatabaseService, IFileService,
│   │                                 IProcessExecutor, IWingetService, IPgPassService
│   ├── Models/                    ← OperationResult<T>, DatabaseConnectionConfig,
│   │                                 OperationProgress, WingetOperationOptions,
│   │                                 PgPassEntry, PgPassResult, etc.
│   ├── Services/                  ← Concrete implementations of all interfaces
│   │   └── ServiceCollectionExtensions.cs  ← AddFurLabServices() DI registration
│   ├── Logging/                   ← Custom ILogger + MicrosoftExtensionsLoggerAdapter
│   ├── HealthChecks/              ← PostgresBinaryHealthCheck, ConfigurationHealthCheck
│   └── Resilience/                ← ResiliencePolicies (Polly retry pipelines)
│
├── FurLab.CLI/                   ← CLI executable (depends on FurLab.Core)
│   ├── Program.cs                 ← Host builder, DI setup, CommandApp configuration
│   ├── Infrastructure/            ← TypeRegistrar and TypeResolver for Spectre.Console.Cli
│   ├── Commands/                  ← Command classes - each subcommand has its own folder
│   │   ├── Files/
│   │   │   └── Combine/
│   │   │       ├── CombineCommand.cs
│   │   │       └── CombineSettings.cs
│   │   ├── Claude/
│   │   │   ├── Install/
│   │   │   │   ├── InstallCommand.cs
│   │   │   │   └── InstallSettings.cs
│   │   │   └── Settings/
│   │   │       ├── McpDatabaseCommand.cs
│   │   │       ├── McpDatabaseSettings.cs
│   │   │       ├── WinEnvCommand.cs
│   │   │       └── WinEnvSettings.cs
│   │   ├── OpenCode/
│   │   │   └── Settings/
│   │   │       ├── McpDatabaseCommand.cs
│   │   │       ├── McpDatabaseSettings.cs
│   │   │       ├── DefaultModelCommand.cs
│   │   │       └── DefaultModelSettings.cs
│   │   ├── Winget/
│   │   │   ├── Backup/
│   │   │   │   ├── BackupCommand.cs
│   │   │   │   └── BackupSettings.cs
│   │   │   └── Restore/
│   │   │       ├── RestoreCommand.cs
│   │   │       └── RestoreSettings.cs
│   │   ├── Database/
│   │   │   ├── Backup/
│   │   │   │   ├── BackupCommand.cs
│   │   │   │   ├── BackupSettings.cs
│   │   │   │   └── BackupConfig.cs
│   │   │   ├── Restore/
│   │   │   │   ├── RestoreCommand.cs
│   │   │   │   ├── RestoreSettings.cs
│   │   │   │   └── RestoreConfig.cs
│   │   │   └── PgPass/
│   │   │       ├── Add/
│   │   │       │   ├── AddCommand.cs
│   │   │       │   └── AddSettings.cs
│   │   │       ├── List/
│   │   │       │   ├── ListCommand.cs
│   │   │       │   └── ListSettings.cs
│   │   │       └── Remove/
│   │   │           ├── RemoveCommand.cs
│   │   │           └── RemoveSettings.cs
│   │   ├── Docker/
│   │   │   └── Postgres/
│   │   │       ├── PostgresCommand.cs
│   │   │       └── PostgresSettings.cs
│   │   ├── Query/
│   │   │   └── Run/
│   │   │       ├── RunCommand.cs
│   │   │       └── RunSettings.cs
│   │   ├── WindowsFeatures/
│   │   │   ├── Export/
│   │   │   │   ├── ExportCommand.cs
│   │   │   │   └── ExportSettings.cs
│   │   │   ├── Import/
│   │   │   │   ├── ImportCommand.cs
│   │   │   │   └── ImportSettings.cs
│   │   │   └── List/
│   │   │       ├── ListCommand.cs
│   │   │       └── ListSettings.cs
│   │   └── Settings/
│   │       └── DbServers/
│   │           ├── List/
│   │           │   ├── ListCommand.cs
│   │           │   └── ListSettings.cs
│   │           ├── Add/
│   │           │   ├── AddCommand.cs
│   │           │   └── AddSettings.cs
│   │           ├── Remove/
│   │           │   ├── RemoveCommand.cs
│   │           │   └── RemoveSettings.cs
│   │           ├── Test/
│   │           │   ├── TestCommand.cs
│   │           │   └── TestSettings.cs
│   │           └── SetPassword/
│   │               ├── SetPasswordCommand.cs
│   │               └── SetPasswordSettings.cs
│   └── SecurityUtils.cs           ← Input validation (path traversal, PostgreSQL identifiers,
│                                     host/port, wildcard *)
│
├── FurLab.Tests/                 ← MSTest project (references FurLab.CLI)
│   └── Commands/                  ← One test class per command
│
├── openspec/                      ← OpenSpec workflow (substituiu specs/ e .specify/)
│   ├── config.yaml                ← Configuração do projeto (schema, idioma pt-BR)
│   ├── specs/                     ← Especificações canônicas por feature
│   └── changes/
│       └── archive/               ← Changes implementados e arquivados
│
├── .opencode/
│   ├── skills/                    ← Skills OpenSpec (openspec-propose, openspec-apply-change, etc.)
│   └── command/                   ← Slash commands customizados
│
├── docs/
│   ├── pt-BR/                     ← Primary documentation (Portuguese, source of truth)
│   └── en/                        ← Secondary documentation (English)
│
└── opencode.json                  ← Configuração local do OpenCode (modelo padrão, etc.)
```

### Command File Organization Convention

**Every subcommand has its own folder.** Commands and their settings follow a strict one-class-per-file structure:

```
Commands/
├── CommandName/                           ← Command group folder (e.g., "Database")
│   ├── SubCommand/                        ← Each subcommand gets its own folder
│   │   ├── {Prefix}Command.cs             ← Command implementation with hierarchy prefix
│   │   ├── {Prefix}Settings.cs            ← Settings class with hierarchy prefix
│   │   └── {Prefix}Config.cs              ← Optional: config class with hierarchy prefix
│   └── SharedHelper.cs                    ← Shared classes across subcommands stay in parent folder
```

**Naming Rules:**
- **Folder structure**: `Commands/{Group}/{Subcommand}/` - every subcommand has its own folder
- **File naming**: Files are prefixed with the hierarchy path (without "Commands" prefix)
  - ✅ `Claude/Install/ClaudeInstallCommand.cs`
  - ✅ `Claude/Install/ClaudeInstallSettings.cs`
  - ✅ `Database/Backup/DatabaseBackupCommand.cs`
  - ✅ `Database/Backup/DatabaseBackupSettings.cs`
  - ✅ `Database/PgPass/Add/PgPassAddCommand.cs`
  - ✅ `Database/PgPass/Add/PgPassAddSettings.cs`
  - ✅ `Settings/DbServers/List/DbServersListCommand.cs`
- **Class naming**: Classes use short names (implied by folder structure)
  - ✅ `class InstallCommand` (in `ClaudeInstallCommand.cs`)
  - ✅ `class BackupSettings` (in `DatabaseBackupSettings.cs`)
  - ✅ `class AddCommand` (in `PgPassAddCommand.cs`)
- **Config files**: Optional config records for internal use
  - ✅ `Database/Backup/DatabaseBackupConfig.cs` → class `BackupConfig`
- **Shared files**: Files used by multiple subcommands of the same group stay in the parent folder
  - ✅ `Database/SharedHelper.cs` - shared across Database subcommands only

**Examples:**
```
Commands/Database/Backup/DatabaseBackupCommand.cs       → class BackupCommand
Commands/Database/Backup/DatabaseBackupSettings.cs      → class BackupSettings
Commands/Database/Backup/DatabaseBackupConfig.cs        → class BackupConfig
Commands/Claude/Settings/McpDatabase/ClaudeSettingsMcpDatabaseCommand.cs   → class McpDatabaseCommand
Commands/Claude/Settings/McpDatabase/ClaudeSettingsMcpDatabaseSettings.cs  → class McpDatabaseSettings
Commands/Settings/DbServers/List/DbServersListCommand.cs → class ListCommand
Commands/Settings/DbServers/List/DbServersListSettings.cs → class ListSettings
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
./publish.ps1 nuget   # Creates .nupkg in bin/Release/net10.0/
./publish.ps1 exe     # Creates win-x64 single-file exe in publish/exe/fur.exe
./publish.ps1 all     # Both

# Install as global tool (from source)
dotnet tool install --add-source bin/Release/net10.0/ FurLab --global

# CLI usage
FurLab --help
FurLab database backup [database] [--host] [--port] [--username] [--password] [--output]
FurLab database restore [database] [file] [connection options]
FurLab database pgpass add <banco> [--host] [--port] [--username] [--password]
FurLab database pgpass list
FurLab database pgpass remove <banco> [--host] [--port] [--username]
FurLab docker postgres
FurLab file combine -i "<glob>" -o <output>
FurLab query run -f <sql-file> -d <db> [--all] [--servers]
FurLab winget backup -o <dir>
FurLab winget restore -i <json-file>
FurLab claude install
FurLab claude settings mcp-database
FurLab claude settings win-env
FurLab opencode settings mcp-database
FurLab opencode settings default-model [model-id] [--global]
FurLab windowsfeatures export -o <file>
FurLab windowsfeatures import -i <file>
FurLab windowsfeatures list
FurLab settings db-servers ls
FurLab settings db-servers add <name> --host <host> --port <port> --username <user> --database <db>
FurLab settings db-servers rm <name>
FurLab settings db-servers test <name>
FurLab settings db-servers set-password <name>
```

## Code Style

- **Language**: C# 13, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Primary constructors**: Use primary constructors in classes and records whenever possible (e.g., `public class Foo(ILogger logger) { }`)
- **Naming**: PascalCase for types/methods/properties; camelCase for locals and parameters
- **Command classes**: Inherit from `Command<TSettings>` or `AsyncCommand<TSettings>`.
- **Command settings**: Define `Settings` classes in separate files within the same directory as the command, naming them `[CommandName]Settings.cs` (e.g., `DatabaseBackupSettings.cs`).
- **Dependency Injection**: Use constructor injection in command classes. All services must be registered in `ServiceCollectionExtensions.cs`.
- **Service classes**: Always implement a `IXxxService` interface in `FurLab.Core/Interfaces/`; concrete implementation in `FurLab.Core/Services/`
- **No business logic in commands**: Commands parse input via `Settings`, then call the service method. Never put logic inside the `Execute` or `ExecuteAsync` method.
- **One file per type**: Each `.cs` file should contain only one class, record, struct, or enum.
- **Result pattern**: Return `OperationResult` / `OperationResult<T>` records — use `SuccessResult(...)` / `FailureResult(...)` factory methods
- **External processes**: Always `UseShellExecute = false`, capture stdout/stderr via redirect — never shell out to `cmd.exe` or `powershell.exe`
- **Test naming**: `<MethodName>_<StateUnderTest>_<ExpectedBehavior>`
- **Unit test attributes**: Use `[TestMethod(DisplayName = "<short description>")]` and `[Description("<detailed description>")]` on all test methods
- **XML doc comments**: Required on all public members (enforced by `GenerateDocumentationFile true`)
- **Configuration**: Read from `%LocalAppData%\FurLab\appsettings.json` via `IUserConfigService`.
- **Security**: Use `SecurityUtils` for validation. Credentials should be handled via `ICredentialService` for secure storage.
- **Async**: All service methods that touch I/O or external processes must be `async Task<T>` and accept `CancellationToken`
- **Progress reporting**: Use `IProgress<OperationProgress>` or Spectre.Console's Status/Progress components for long-running operations
- **Logging**: Use `ILogger<T>` (Microsoft.Extensions.Logging) in CLI and `ILogger` (custom) in Core.

## Architecture Constraints

- Dependencies flow **downward only**: `Tests` → `CLI` → `Core`. Never reference a higher layer from a lower one.
- New NuGet packages require justification.
- All inputs validated before use via `SecurityUtils`.
- Exit codes: `0` success, `1` general error, `2` invalid args, `10-11` Database error, `20-22` I/O error, `30` Access error, `130` user cancellation.

## OpenSpec Workflow

O projeto usa o workflow **OpenSpec** para gerenciar features e mudanças:

- **`openspec/config.yaml`** — schema e configurações (idioma pt-BR obrigatório)
- **`openspec/specs/<slug>/spec.md`** — especificações canônicas por feature
- **`openspec/changes/`** — mudanças em andamento
- **`openspec/changes/archive/`** — mudanças implementadas e arquivadas

## Recent Changes

- migrate-cli-to-spectre-console-cli (2026-04-18): Migrated from `System.CommandLine` to `Spectre.Console.Cli` for better TUI support and cleaner command structure.
- secure-credential-storage (2026-04-13): Implemented `ICredentialService` for encrypted storage of database passwords.
- query-run-multi-server (2026-04-13): `query run` now supports executing scripts across multiple servers defined in settings.
- docker-postgres (2026-04-05): Added `docker postgres` command.
- opencode-default-model (2026-04-07): Define default model for OpenCode.

## Guard Rails (Agent Safety Protocol)

> **Mandatory Rule:** This repository enforces a strict **Manual Confirmation Policy** for all state-changing Git operations.

### 1. Restricted Mutations (Explicit Permission Required)
AI Agents **MUST NOT** execute or suggest the following commands without prior, explicit user authorization:
- **Index/Commit:** `git add`, `git commit`, `git rm`, `git mv`
- **History/Sync:** `git push`, `git pull`, `git fetch`, `git merge`, `git rebase`, `git reset`, `git cherry-pick`
- **Branching:** `git checkout` (if files are modified), `git branch -d`
- **State:** `git stash`
- **Filesystem:** Deletion or destructive modification of tracked files.

### 2. Authorized Read-Only Operations
Agents are permitted to run these commands autonomously for context discovery:
- `git status`, `git log`, `git diff`, `git show`, `git branch -a`, `git rev-parse`

### 3. Execution Workflow
When a Git mutation is necessary to fulfill a request:
1. **Analyze:** Determine the exact commands needed.
2. **Disclose:** Explain the command and its intended impact on the repository.
3. **Prompt:** Request explicit confirmation (e.g., "Confirm execution of `git commit`? [y/N]").
4. **Await:** Wait for positive user confirmation before invoking the tool.
