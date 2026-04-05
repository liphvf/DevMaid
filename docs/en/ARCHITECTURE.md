# Architecture Documentation

## Architecture Overview

DevMaid is a .NET-based command-line interface (CLI) tool designed using a modular, command-based architecture. The application follows the principles of separation of concerns, with clear boundaries between CLI parsing, business logic, and data access layers.

The architecture is built on top of System.CommandLine for CLI argument parsing and Microsoft.Extensions.Configuration for flexible configuration management.

## High-Level Design

```
┌─────────────────────────────────────────────────────────────────┐
│                        DevMaid CLI                              │
├─────────────────────────────────────────────────────────────────┤
│  Entry Point (Program.cs)                                       │
│  ├── Configuration Loading                                      │
│  ├── Command Registration                                       │
│  └── Argument Parsing                                           │
├─────────────────────────────────────────────────────────────────┤
│  Commands Layer (Commands/)                                      │
│  ├── TableParserCommand                                         │
│  ├── FileCommand                                                │
│  ├── ClaudeCodeCommand                                          │
│  ├── OpenCodeCommand                                            │
│  ├── WingetCommand                                              │
│  ├── QueryCommand                                               │
│  ├── CleanCommand                                               │
│  └── WindowsFeaturesCommand                                     │
├─────────────────────────────────────────────────────────────────┤
│  Support Layers                                                 │
│  ├── CommandOptions (DTOs)                                      │
│  ├── Database (Npgsql)                                          │
│  └── Utils (Helper Functions)                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components and Responsibilities

### 1. Program.cs (Entry Point)

**Responsibilities:**
- Initialize application configuration
- Register all available commands
- Parse command-line arguments
- Invoke the appropriate command handler

**Key Methods:**
- `Main(string[] args)` - Application entry point
- ConfigurationBuilder setup with JSON, environment variables, and user secrets

### 2. Commands Layer

Each command follows the builder pattern with a static `Build()` method that returns a `Command` object.

#### TableParserCommand
- Connects to PostgreSQL database using Npgsql
- Retrieves table metadata
- Generates C# class with properties based on column definitions

#### FileCommand (Combine)
- Combines multiple files into one
- Supports file patterns with wildcards
- Preserves encoding from source files

#### ClaudeCodeCommand
- Installs Claude Code via winget
- Configures MCP database settings
- Sets up Windows environment for Claude

#### OpenCodeCommand
- Installs OpenCode CLI
- Checks installation status
- Configuration management

#### WingetCommand
- Exports installed packages to JSON
- Imports packages from backup
- Cross-package dependency resolution

#### QueryCommand
- Executes SQL queries and exports results to CSV
- Supports multiple databases and servers configuration via appsettings.json

#### CleanCommand
- Recursively cleans bin and obj folders from the current working directory or solution
- Frees up space and solves compilation caching issues

#### WindowsFeaturesCommand
- Exports currently activated Windows optional features to JSON
- Imports features from JSON using dism.exe
- Allows listing activated features

### 3. CommandOptions Layer

Data Transfer Objects (DTOs) that represent command-line options:
- Strongly typed option classes
- Validation attributes
- Default value handling

## Data Flow

### CLI Execution Flow

### 1. Builder Pattern

Each command implements a static `Build()` method that constructs and configures the command object:

```csharp
public static Command Build()
{
    var command = new Command("winget", "Manage winget packages.");
    // Add options and subcommands
    return command;
}
```

### 2. Singleton Pattern (Configuration)

The `Program.AppSettings` property provides centralized access to application configuration.

### 3. Observer Pattern (Process Events)

Real-time output capture uses event handlers:
- `OutputDataReceived`
- `ErrorDataReceived`

## Technical Decisions

### 1. System.CommandLine for CLI Parsing

**Decision:** Use System.CommandLine instead of manual parsing or third-party libraries.

**Rationale:**
- Built-in .NET library
- Strongly typed options
- Built-in help generation
- Supports subcommands

### 2. Npgsql for Database Access

**Decision:** Use Npgsql as the PostgreSQL provider.

**Rationale:**
- Official PostgreSQL .NET driver
- High performance
- Full PostgreSQL feature support
- Active maintenance

### 3. Microsoft.Extensions.Configuration

**Decision:** Use the configuration extensions for flexible settings.

**Rationale:**
- Multiple configuration sources (JSON, environment variables, user secrets)
- Strong typing with configuration binding
- Industry standard pattern

## Scalability Considerations

### Command Extensibility

The architecture supports easy addition of new commands:
1. Create a new command class in `Commands/`
2. Implement the `Build()` method
3. Register in `Program.cs`

### Configuration Scalability

The configuration system supports:
- Multiple environment-specific settings files
- Environment variable overrides
- User secrets for sensitive data

## Security Considerations

### 1. Password Handling

- Passwords can be provided via command line or prompted securely
- No password logging or persistence
- User secrets support for development

### 2. Process Execution

- Commands run with the same privileges as the user
- No shell execution (UseShellExecute = false)
- Output captured and sanitized

### 3. Configuration Security

- Sensitive data stored in user secrets
- Environment variables for deployment
- No hardcoded credentials

## Future Architectural Improvements

### 1. Plugin System

Implement a plugin architecture to allow third-party command extensions:
- Separate assemblies for commands
- Dynamic command discovery
- Version compatibility checks

### 2. Configuration API

Expose configuration via API for integration with other tools:
- REST endpoint for configuration queries
- Hot-reload configuration changes

### 3. Progress Reporting Framework

Create a unified progress reporting system:
- Consistent progress UI across commands
- Cancellation support
- ETA calculations

### 4. Logging Framework

Add structured logging:
- File-based logging
- Log levels
- Log rotation
- Integration with external log aggregators

### 5. Unit Test Infrastructure

Improve test coverage:
- Command unit tests
- Integration tests for database operations

### 6. Cross-Platform Support

Expand beyond Windows:
- macOS/Linux winget alternatives support
- Platform-specific command implementations

## Directory Structure

```
DevMaid/
├── DevMaid.CLI/               # Command line app project
│   ├── Program.cs             # Entry point
│   ├── Commands/              # Command implementations (TableParser, Winget, Query, etc.)
│   ├── CommandOptions/        # Commands options and DTOs
│   └── Services/              # Services like Logging, Database listing, etc.
├── DevMaid.Core/              # Main library containing shared logic
│   ├── Interfaces/            # Contracts (ILogger, IFileService, etc.)
│   └── Services/              # Core business services
├── DevMaid.Tests/             # Testing package (MSTest)
└── docs/                      # Documentation
    ├── en/                    # English Documentation
    │   ├── ARCHITECTURE.md
    │   └── FEATURE_SPECIFICATION.md
    └── pt-BR/                 # Portuguese Documentation
        ├── ARCHITECTURE.md
        └── FEATURE_SPECIFICATION.md
```

## Conclusion

DevMaid's architecture provides a solid foundation for a CLI tool with:
- Clean separation of concerns
- Easy extensibility
- Maintainable codebase
- Flexible configuration management

The modular design allows for easy addition of new features while maintaining code quality and testability.
