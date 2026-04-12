# FurLab Development Guidelines

Auto-generated from codebase. Last updated: 2026-04-07

## Idioma

Todo output para o usuário deve ser em **português do Brasil (pt-BR)**.
Isso inclui explicações, resumos, mensagens de erro, perguntas de esclarecimento e qualquer comunicação direta com o usuário.
Exceção: não traduza nomes de skills, comandos do openspec (ex: `openspec-propose`, `opsx-apply`), identificadores de código, nomes de arquivos ou comandos de terminal.

## Active Technologies

- C# 13 / .NET 10 (`net10.0`)
- System.CommandLine 2.0.5 — CLI argument parsing and subcommand routing
- Npgsql 10.0.2 — PostgreSQL ADO.NET driver
- CsvHelper 33.1.0 — CSV export for query results
- Spectre.Console.Cli 0.55.0 — Interactive TUI prompts (selection menus, styled output)
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
│   ├── Interfaces/                ← IConfigurationService, IDatabaseService, IFileService,
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
│   ├── Program.cs                 ← Host builder, DI setup, RootCommand registration
│   ├── Commands/                  ← One static class per command, Build() factory method
│   │   ├── FileCommand.cs
│   │   ├── ClaudeCodeCommand.cs
│   │   ├── OpenCodeCommand.cs
│   │   ├── WingetCommand.cs
│   │   ├── DatabaseCommand.cs
│   │   ├── PgPassCommand.cs       ← Subcomando de DatabaseCommand (pgpass add/list/remove)
│   │   ├── DockerCommand.cs       ← Docker utilities (postgres container)
│   │   ├── QueryCommand.cs
│   │   ├── CleanCommand.cs
│   │   └── WindowsFeaturesCommand.cs
│   ├── CommandOptions/            ← Strongly-typed options DTOs per command
│   ├── Services/                  ← Static facade wrappers (ConfigurationService, Logger,
│   │                                 DockerService, DockerConstants, PostgresPasswordHandler)
│   └── SecurityUtils.cs           ← Input validation (path traversal, PostgreSQL identifiers,
│                                     host/port, wildcard *)
│
├── FurLab.Tests/                 ← MSTest project (references FurLab.CLI)
│   └── Commands/                  ← One test class per command
│
├── openspec/                      ← OpenSpec workflow (substituiu specs/ e .specify/)
│   ├── config.yaml                ← Configuração do projeto (schema, idioma pt-BR)
│   ├── specs/                     ← Especificações canônicas por feature
│   │   ├── pgpass-cli-setup/
│   │   ├── pgpass-wildcard-validation/
│   │   ├── docker-postgres/
│   │   └── opencode-default-model/
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
FurLab database backup <dbname> [--host] [--port] [--username] [--password] [--output]
FurLab database restore <dbname> --input <file> [connection options]
FurLab database backup --all [connection options]
FurLab database pgpass add <banco> [--host] [--port] [--username] [--password]
FurLab database pgpass list
FurLab database pgpass remove <banco> [--host] [--port] [--username]
FurLab docker postgres
FurLab file combine -i "<glob>" -o <output>
FurLab query run -f <sql-file> -d <db> [--all] [--servers]
FurLab clean [path]
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
```

## Code Style

- **Language**: C# 13, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Primary constructors**: Use primary constructors in classes and records whenever possible (e.g., `public class Foo(ILogger logger) { }`) to reduce boilerplate and improve readability
- **Naming**: PascalCase for types/methods/properties; camelCase for locals and parameters
- **Command classes**: Always `static`, expose `static Build(): Command` factory — never inherit
- **Service classes**: Always implement a `IXxxService` interface in `FurLab.Core/Interfaces/`; concrete implementation in `FurLab.Core/Services/`
- **No business logic in commands**: Commands parse input into a typed Options DTO, then call the service method. Never put logic inside the command handler lambda.
- **One file per type**: Each `.cs` file should contain only one class, record, struct, or enum. Multiple types in the same file are discouraged; split into separate files with matching names (e.g., `DatabaseBackupConfig.cs`, `DatabaseRestoreConfig.cs`).
- **Result pattern**: Return `OperationResult` / `OperationResult<T>` records — use `SuccessResult(...)` / `FailureResult(...)` factory methods
- **External processes**: Always `UseShellExecute = false`, capture stdout/stderr via redirect — never shell out to `cmd.exe` or `powershell.exe`
- **Test naming**: `<MethodName>_<StateUnderTest>_<ExpectedBehavior>` (e.g., `BackupAsync_ComOpcoesValidas_DeveCriarArquivoDump`)
- **Unit test attributes**: Use `[TestMethod(DisplayName = "<short description>")]` and `[Description("<detailed description>")]` on all test methods for better reporting:
  ```csharp
  [TestMethod(DisplayName = "Backup with valid options should succeed")]
  [Description("Verifies that when pg_dump is available and valid options are provided, the backup operation completes without throwing exceptions.")]
  public void Backup_ValidOptions_DoesNotThrow()
  ```
- **XML doc comments**: Required on all public members (enforced by `GenerateDocumentationFile true`)
- **Configuration**: Read from `%LocalAppData%\FurLab\appsettings.json`; never hard-code connection strings; use `SecurityUtils.IsValidPath()` and `SecurityUtils.IsValidPostgreSQLIdentifier()` before using any user-supplied input; `SecurityUtils.IsValidHost()` and `SecurityUtils.IsValidPort()` para conexões; `*` é aceito como curinga em host/port/username no pgpass
- **Async**: All service methods that touch I/O or external processes must be `async Task<T>` and accept `CancellationToken`
- **Progress reporting**: Use `IProgress<OperationProgress>` for operations expected to take > 2s
- **Logging**: Use `ILogger` from `FurLab.Core/Logging/ILogger.cs` — not `Microsoft.Extensions.Logging.ILogger` directly in Core

## Architecture Constraints

- Dependencies flow **downward only**: `Tests` → `CLI` → `Core`. Never reference a higher layer from a lower one.
- New NuGet packages require justification in the feature spec's "Decisões Técnicas" section.
- New features may introduce at most **one new project** — additional projects require explicit approval.
- All inputs validated before use: paths via `SecurityUtils.IsValidPath()`, PostgreSQL identifiers via `SecurityUtils.IsValidPostgreSQLIdentifier()`.
- Exit codes: `0` success, `1` general error, `2` invalid args, `3` external dependency not found, `130` user cancellation.

## OpenSpec Workflow

O projeto usa o workflow **OpenSpec** para gerenciar features e mudanças:

- **`openspec/config.yaml`** — schema e configurações (idioma pt-BR obrigatório em todos os artefatos)
- **`openspec/specs/<slug>/spec.md`** — especificações canônicas por feature (substituiu `specs/`)
- **`openspec/changes/`** — mudanças em andamento (proposal, design, tasks)
- **`openspec/changes/archive/`** — mudanças implementadas e arquivadas

Skills disponíveis em `.opencode/skills/`:
- `openspec-propose` — cria proposta completa com todos os artefatos
- `openspec-apply-change` — implementa tasks de uma mudança
- `openspec-continue-change` — avança para o próximo artefato
- `openspec-ff-change` — cria todos os artefatos de uma vez
- `openspec-verify-change` — valida implementação antes de arquivar
- `openspec-archive-change` — arquiva mudança concluída
- `openspec-sync-specs` — sincroniza delta specs com specs principais
- `openspec-explore` — modo de exploração / pensamento colaborativo

## Recent Changes

- docker-postgres (2026-04-05): Added `DockerCommand` — `docker postgres` inicia/cria container PostgreSQL local via Docker
- pgpass-wildcard-fields (2026-04-04): `SecurityUtils.IsValidHost/Port/Username` aceitam `*` como curinga; `database pgpass add/list/remove` suportam wildcard
- pgpass-cli-setup (2026-04-04): Added `PgPassCommand` — gerenciamento de `pgpass.conf` com subcomandos `add`, `list`, `remove`; `IPgPassService` + `PgPassService`
- opencode-default-model (2026-04-07): Added `opencode settings default-model` — define modelo padrão via argumento ou menu interativo (Spectre.Console); suporte a `--global`
- 009-windows-features-manager: Added `WindowsFeaturesCommand` — dism.exe wrapper for export/import/list of Windows optional features
- 008-project-cleaner: Added `CleanCommand` — recursive `bin/` and `obj/` directory deletion
- 007-sql-query-csv-export: Added `QueryCommand` — SQL file execution with CSV export, multi-server support

## Guard Rails

### 🚨 CRITICAL: Git Safety

**NEVER** perform or suggest Git operations that modify repository state without **explicit user confirmation**.

#### Prohibited Operations (unless explicitly asked):
- `git add`, `git commit`, `git push`
- `git merge`, `git rebase`, `git reset`
- `git checkout` (if it changes files)
- `git stash`, `git cherry-pick`
- Deleting or modifying tracked files
- Force push to main/master branches

#### Required Behavior:
1. **Never run Git commands automatically**
2. **Never assume permission** to modify version control
3. **Always ask for confirmation** before suggesting any Git operation
4. **Explain consequences** before executing any Git command

#### Exception - Safe Read-Only Commands:
These are allowed without confirmation:
- `git status`
- `git log`
- `git diff`
- `git show`
- `git branch -a`

#### If Git Action Is Required:
```
1. Explain what will happen
2. Ask for explicit confirmation
3. Only then proceed (if confirmed)
```

---
