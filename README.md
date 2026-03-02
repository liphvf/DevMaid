# DevMaid

A powerful .NET CLI tool to automate common development tasks.

## Description

DevMaid is a cross-platform command-line interface (CLI) built with .NET that helps developers automate repetitive development tasks. It provides commands for database operations, file management, AI tool installation (Claude Code, OpenCode), and Windows package management.

> **Note**: This is a hobby project created for personal use. It may not follow all best practices or have comprehensive testing. Contributions and feedback are welcome, but please keep in mind this was built to solve the author's specific needs.

## Problem It Solves

Developers often perform repetitive tasks that can be automated:
- Converting database table schemas to C# classes
- Combining multiple files into one
- Installing and configuring AI development tools
- Backing up and restoring Windows packages

DevMaid consolidates these tasks into a single, easy-to-use CLI tool.

## Key Features

- **Table Parser**: Parse PostgreSQL database tables and generate C# property classes
- **File (Combine)**: Combine multiple files into one
- **Claude Code Integration**: Install and configure Claude Code CLI
- **OpenCode Integration**: Install and configure OpenCode CLI
- **Winget Manager**: Backup and restore Windows package manager packages
- **Interactive TUI Mode**: User-friendly terminal interface with navigation

## Tech Stack

- **Framework**: .NET 10
- **Language**: C#
- **CLI Parsing**: System.CommandLine
- **TUI**: Terminal.Gui
- **Database**: Npgsql (PostgreSQL)
- **Configuration**: Microsoft.Extensions.Configuration

## Installation

### Prerequisites

- .NET SDK 10 or later
- Windows (required for Claude, OpenCode, and Winget commands)

### Install as .NET Tool

```bash
dotnet tool install --global DevMaid
```

Or install from NuGet:

```bash
dotnet tool install -g DevMaid
```

### Build from Source

```bash
git clone https://github.com/your-repo/DevMaid.git
cd DevMaid
dotnet restore
dotnet build
```

## How to Run Locally

### Run from Source

```bash
dotnet run -- --help
```

### Run TUI Mode

```bash
devmaid tui
```

## Basic Usage Examples

### Table Parser - Generate C# Class from Database Table

```bash
devmaid table-parser -d mydb -t users -u postgres -H localhost
```

### Combine Files

```bash
devmaid file combine -i "C:\temp\*.sql" -o "C:\temp\result.sql"
```

### Install Claude Code

```bash
devmaid claude install
```

### Winget Backup

```bash
devmaid winget backup -o "C:\backup"
```

### Winget Restore

```bash
devmaid winget restore -i "C:\backup\backup-winget.json"
```

### Interactive TUI Mode

```bash
devmaid tui
```

Use arrow keys to navigate, Enter to select, Esc to exit.

## Command List

| Command | Description |
|---------|-------------|
| `table-parser` | Parse database table to C# class |
| `file combine` | Combine multiple files into one |
| `claude` | Claude Code integration |
| `opencode` | OpenCode CLI integration |
| `winget` | Windows package manager |
| `tui` | Launch interactive TUI mode (Experimental) |

## Documentation

For more detailed information, see:

- [Architecture](./docs/en/ARCHITECTURE.md)
- [Feature Specification](./docs/en/FEATURE_SPECIFICATION.md)

## Contribution

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure all tests pass and code follows the project's coding standards.

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

---

🇺🇸 English (default)  
🇧🇷 Portuguese: [README.pt-BR.md](./README.pt-BR.md)
