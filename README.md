<div align="center">
  <img src="assets/FurLab_icon.png" alt="FurLab Logo" width="160" />

  # FurLab

  A powerful .NET CLI tool to automate common development tasks.
</div>

## Description

FurLab is a cross-platform command-line interface (CLI) built with .NET that helps developers automate repetitive development tasks. It provides commands for database operations, file management, AI tool installation (Claude Code, OpenCode), and Windows package management.

> **Note**: This is a hobby project created for personal use. It may not follow all best practices or have comprehensive testing. Contributions and feedback are welcome, but please keep in mind this was built to solve the author's specific needs.

## Problem It Solves

Developers often perform repetitive tasks that can be automated:
- Combining multiple files into one
- Installing and configuring AI development tools
- Backing up and restoring Windows packages

FurLab consolidates these tasks into a single, easy-to-use CLI tool.

## Key Features

- **Database Backup**: Backup PostgreSQL databases using pg_dump
- **File (Combine)**: Combine multiple files into one
- **Claude Code Integration**: Install and configure Claude Code CLI
- **OpenCode Integration**: Install and configure OpenCode CLI
- **Winget Manager**: Backup and restore Windows package manager packages

## Tech Stack

- **Framework**: .NET 10
- **Language**: C#
- **CLI Parsing**: System.CommandLine
- **Database**: Npgsql (PostgreSQL)
- **Configuration**: Microsoft.Extensions.Configuration

## Installation

### Prerequisites

- .NET SDK 10 or later
- Windows (required for Claude, OpenCode, and Winget commands)

### Install as .NET Tool

```bash
dotnet tool install --global FurLab
```

Or install from NuGet:

```bash
dotnet tool install -g FurLab
```

### Build from Source

```bash
git clone https://github.com/your-repo/FurLab.git
cd FurLab
dotnet restore
dotnet build
```

## How to Run Locally

### Run from Source

```bash
dotnet run -- --help
```

## Basic Usage Examples

### Database Backup

```bash
# Backup with default connection settings (from appsettings.json)
FurLab database backup mydb

# Backup with custom connection settings
FurLab database backup mydb --host localhost --port 5432 --username postgres --password mypassword

# Backup with custom output path
FurLab database backup mydb -o "C:\backups\mydb.backup"

# Backup with password prompt (password not provided in command line)
FurLab database backup mydb --host localhost --username postgres
```

**Configuration File**: Create an `appsettings.json` in `%LocalAppData%\FurLab\` to set default connection values:

```json
{
  "Database": {
    "Host": "localhost",
    "Port": "5432",
    "Username": "postgres",
    "Password": ""
  }
}
```


```bash
```

### Combine Files

```bash
FurLab file combine -i "C:\temp\*.sql" -o "C:\temp\result.sql"
```

### Install Claude Code

```bash
FurLab claude install
```

### Winget Backup

```bash
FurLab winget backup -o "C:\backup"
```

### Winget Restore

```bash
FurLab winget restore -i "C:\backup\backup-winget.json"
```

## Command List

| Command | Description |
|---------|-------------|
| `database backup` | Backup PostgreSQL database |
| `file combine` | Combine multiple files into one |
| `claude` | Claude Code integration |
| `opencode` | OpenCode CLI integration |
| `winget` | Windows package manager |

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
