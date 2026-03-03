# Architecture Documentation

## Architecture Overview

DevMaid is a .NET-based command-line interface (CLI) tool designed using a modular, command-based architecture. The application follows the principles of separation of concerns, with clear boundaries between CLI parsing, business logic, and data access layers.

The architecture is built on top of System.CommandLine for CLI argument parsing, Terminal.Gui for the interactive TUI mode, and Microsoft.Extensions.Configuration for flexible configuration management.

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
│  └── TuiCommand                                                 │
├─────────────────────────────────────────────────────────────────┤
│  TUI Layer (Tui/)                                               │
│  ├── TuiApp (Main Application)                                  │
│  └── MenuItem (Data Model)                                     │
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

#### TuiCommand
- Launches the interactive Terminal User Interface
- Entry point for TuiApp

### 3. TUI Layer (Tui/)

#### TuiApp
- Main TUI application controller
- Manages menu navigation and rendering
- Handles real-time command execution with progress display
- Detects terminal theme (light/dark)

#### MenuItem
- Data model for menu entries
- Contains Name, Description, and Action properties

### 4. CommandOptions Layer

Data Transfer Objects (DTOs) that represent command-line options:
- Strongly typed option classes
- Validation attributes
- Default value handling

## Data Flow

### CLI Execution Flow

```
User Input
    ↓
System.CommandLine Parser
    ↓
Command Handler (e.g., WingetCommand.RunBackup)
    ↓
Business Logic
    ↓
External Service (Winget, PostgreSQL, File System)
    ↓
Output/Result
```

### TUI Execution Flow

```
User Launches TUI
    ↓
TuiApp.Run()
    ↓
Detect Terminal Theme
    ↓
Render Main Menu
    ↓
User Navigation (Arrow Keys)
    ↓
Selection → Execute Action
    ↓
Show Progress/Output Dialog
    ↓
Return to Menu / Exit
```

### Real-Time Command Execution Flow

```
User Selects Command
    ↓
Show Progress Dialog
    ↓
Start Process (async)
    ↓
Capture Output (OutputDataReceived)
    ↓
Update UI (Application.MainLoop.Invoke)
    ↓
Wait for Completion
    ↓
Show Exit Code
    ↓
Allow User to Close
```

## Design Patterns Used

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

### 3. Strategy Pattern (TUI Theme)

The `DetectTerminalTheme()` method determines the appropriate color scheme based on terminal detection, with separate strategies for light and dark themes.

### 4. Command Pattern (MenuItems)

Each menu item encapsulates an action that can be executed:

```csharp
new MenuItem("Name", "Description", () => RunCommand("..."))
```

### 5. Observer Pattern (Process Events)

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

### 2. Terminal.Gui for TUI

**Decision:** Use Terminal.Gui for the interactive terminal interface.

**Rationale:**
- Mature and well-maintained
- Cross-platform support
- Declarative API
- Good documentation

### 3. Npgsql for Database Access

**Decision:** Use Npgsql as the PostgreSQL provider.

**Rationale:**
- Official PostgreSQL .NET driver
- High performance
- Full PostgreSQL feature support
- Active maintenance

### 4. Microsoft.Extensions.Configuration

**Decision:** Use the configuration extensions for flexible settings.

**Rationale:**
- Multiple configuration sources (JSON, environment variables, user secrets)
- Strong typing with configuration binding
- Industry standard pattern

### 5. Async Process Execution in TUI

**Decision:** Execute external commands asynchronously with real-time UI updates.

**Rationale:**
- Non-blocking UI
- Real-time output display
- Better user experience for long-running commands

## Scalability Considerations

### Command Extensibility

The architecture supports easy addition of new commands:
1. Create a new command class in `Commands/`
2. Implement the `Build()` method
3. Register in `Program.cs`

### TUI Menu Extensibility

Adding new menu items is straightforward:
1. Create MenuItem with name, description, and action
2. Add to the appropriate menu list

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
- TUI interaction tests

### 6. Cross-Platform Support

Expand beyond Windows:
- macOS/Linux winget alternatives support
- Platform-specific command implementations

## Directory Structure

```
DevMaid/
├── Program.cs                 # Entry point
├── DevMaid.csproj            # Project file
├── Commands/                  # Command implementations
│   ├── TuiCommand.cs
│   ├── TableParserCommand.cs
│   ├── FileCommand.cs
│   ├── ClaudeCodeCommand.cs
│   ├── OpenCodeCommand.cs
│   └── WingetCommand.cs
├── CommandOptions/            # DTOs for commands
├── Tui/                       # TUI components
│   ├── TuiApp.cs
│   └── MenuItem.cs
├── Utils.cs                   # Helper functions
├── Database.cs               # Database utilities
└── docs/                     # Documentation
    ├── en/
    │   ├── ARCHITECTURE.md
    │   └── FEATURE_SPECIFICATION.md
    └── pt-BR/
        ├── ARCHITECTURE.md
        └── FEATURE_SPECIFICATION.md
```

## Conclusion

DevMaid's architecture provides a solid foundation for a CLI tool with:
- Clean separation of concerns
- Easy extensibility
- Maintainable codebase
- Good user experience through TUI mode
- Flexible configuration management

The modular design allows for easy addition of new features while maintaining code quality and testability.
