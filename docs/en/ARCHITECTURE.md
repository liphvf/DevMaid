# Architecture Documentation

## Architecture Overview

FurLab is a .NET-based Command Line Interface (CLI) tool designed using a command-based modular architecture. The application follows separation of concerns principles, with clear boundaries between CLI parsing, business logic, and data access layers.

The architecture is built on **Spectre.Console.Cli** for CLI argument parsing and rich UI, and **Microsoft.Extensions.DependencyInjection** for dependency management.

## High-Level Design

```
┌─────────────────────────────────────────────────────────────────┐
│                        FurLab CLI                              │
├─────────────────────────────────────────────────────────────────┤
│  Entry Point (Program.cs)                                       │
│  ├── Host Configuration (DI)                                    │
│  ├── Command Registration (CommandApp)                          │
│  └── Global Exception Handling                                  │
├─────────────────────────────────────────────────────────────────┤
│  Command Layer (Commands/)                                      │
│  ├── File/ (FileCombineCommand, FilesConvertEncodingCommand)   │
│  ├── Claude/ (Install, Settings)                                │
│  ├── OpenCode/ (Settings)                                       │
│  ├── Winget/ (Backup, Restore)                                  │
│  ├── Database/ (Backup, Restore, PgPass)                        │
│  ├── Docker/ (Postgres)                                         │
│  ├── Query/ (QueryRunCommand)                                   │
│  ├── WindowsFeatures/ (Export, Import, List)                    │
│  └── Settings/ (DbServers)                                      │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure (Infrastructure/)                               │
│  ├── TypeRegistrar (Adapter for Spectre.Console.Cli)            │
│  └── TypeResolver                                               │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components and Responsibilities

### 1. Program.cs (Entry Point)

**Responsibilities:**
- Configure the Dependency Injection (DI) container
- Configure the Spectre.Console `CommandApp` application
- Define the command and subcommand hierarchy (branches)
- Implement global exception mapping to exit codes

### 2. Command Layer

Each command is a class that inherits from `Command<TSettings>` or `AsyncCommand<TSettings>`.

- **Settings**: Nested class `public sealed class Settings : CommandSettings` that defines command arguments and options using attributes like `[CommandArgument]` and `[CommandOption]`.
- **Dependency Injection**: Commands receive services via constructor.
- **Execution**: Command logic resides in the `Execute` or `ExecuteAsync` method, which should only validate inputs (via Settings) and delegate execution to the appropriate services in `FurLab.Core`.

### 3. Infrastructure Layer

Provides the bridge between the .NET DI container (`IServiceCollection`) and Spectre.Console.Cli through the `TypeRegistrar` and `TypeResolver` classes.

## Technical Decisions

### 1. Spectre.Console.Cli for CLI Parsing

**Decision:** Replace `System.CommandLine` with `Spectre.Console.Cli`.

**Rationale:**
- Better support for TUI interfaces (Tables, Progress, Status, Interactive Prompts)
- Native integration with dependency injection
- Class-based command definition, facilitating maintenance and testing
- Automatic and simplified ANSI formatting

### 2. Native Dependency Injection

**Decision:** Use `Microsoft.Extensions.DependencyInjection`.

**Rationale:**
- Official .NET standard
- Facilitates decoupling between CLI and business logic
- Allows for service replacement with Mocks in unit tests

### 3. Secure Credential Storage

**Decision:** Implement `ICredentialService` to manage database passwords.

**Rationale:**
- Avoids storing passwords in plain text in `appsettings.json`
- Protects sensitive user data through encryption (Windows Data Protection API or similar)

## Security Considerations

### 1. Input Validation

- Strict use of `SecurityUtils` to validate file paths, database identifiers, hosts, and ports.
- Prevention of Path Traversal and SQL Injection through string sanitization.

### 2. Password Handling

- Passwords are never logged or displayed in plain text.
- Interactive prompts mask user input.
- Use of `pgpass.conf` and `ICredentialService` to avoid passing passwords via command-line arguments (which are visible in shell history).

## Directory Structure

```
FurLab/
├── FurLab.CLI/               # UI Project (Spectre.Console)
│   ├── Program.cs             # Configuration and Command Registration
│   ├── Commands/              # Command classes (Organized by subdirectory)
│   ├── Infrastructure/        # DI Adapters for the CLI
│   └── SecurityUtils.cs       # Security Validators
├── FurLab.Core/              # Business Logic and Contracts
│   ├── Interfaces/            # Service Interfaces
│   ├── Services/              # Service Implementations
│   ├── Models/                # DTOs and Data Models
│   └── Logging/               # Logging Abstraction
├── FurLab.Tests/             # MSTest + Moq Tests
└── docs/                      # Documentation (pt-BR and en)
```
