# Feature Specification Documentation

## Product Overview

DevMaid is a .NET-based CLI tool designed to automate common development tasks. It provides a unified interface for database operations, file management, AI tool installation, and Windows package management.

The tool offers two modes of operation:
1. **CLI Mode**: Direct command-line execution
2. **TUI Mode**: Interactive terminal user interface with menus

## Feature List

### Core Features

1. **Table Parser**
2. **File Utilities**
3. **Claude Code Integration**
4. **OpenCode Integration**
5. **Winget Package Manager**
6. **Interactive TUI Mode**

---

## Feature 1: Table Parser

### Objective

Parse PostgreSQL database tables and automatically generate C# class definitions with properties matching table columns.

### Detailed Description

The Table Parser connects to a PostgreSQL database, retrieves metadata for a specified table, and generates a C# class with properties corresponding to the table's columns.

### Usage Flow

```bash
devmaid table-parser -d database -t users -u postgres -H localhost
```

1. User provides database connection parameters
2. Tool connects to PostgreSQL database
3. Tool queries table metadata (column names, types, nullable)
4. Tool generates C# class with appropriate property types
5. Output is saved to file

### Business Rules

- Database connection requires valid credentials
- Table must exist in the specified database
- Column types are mapped to equivalent C# types:
  - `int` → `int`
  - `varchar(n)` → `string`
  - `timestamp` → `DateTime`
  - `boolean` → `bool`
  - `numeric` → `decimal`
  - etc.
- Nullable columns generate nullable C# properties (`int?`, `string?`, etc.)
- Primary key detection for potential Id property

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Invalid database credentials | Display error message, exit with code 1 |
| Table not found | Display "Table not found" error |
| Connection timeout | Display timeout error with retry suggestion |
| Unsupported column type | Use `object` type with warning comment |
| Empty table | Generate empty class with comment |

### Options

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `-d`, `--database` | Yes | - | Database name |
| `-t`, `--table` | No | - | Table name |
| `-u`, `--user` | No | postgres | Database user |
| `-p`, `--password` | No | - | Database password (prompted if not provided) |
| `-H`, `--host` | No | localhost | Database host |
| `-o`, `--output` | No | ./table.class | Output file path |

---

## Feature 2: File Utilities

### Objective

Provide file management utilities for searching, organizing, and finding duplicates.

### Detailed Description

File utilities help developers manage files in their projects through search, organization by extension, and duplicate detection.

### Sub-Features

#### 2.1 Search Files

Find files by name or pattern in a directory tree.

#### 2.2 Organize by Extension

Group files into directories based on their file extension.

#### 2.3 Find Duplicates

Identify duplicate files based on content hash.

### Usage Flow

```bash
# Search files
devmaid file search --pattern "*.cs" --path "C:\project"

# Organize files
devmaid file organize --path "C:\downloads"

# Find duplicates
devmaid file duplicates --path "C:\photos"
```

### Business Rules

- Search supports wildcards (* and ?)
- Organize creates subdirectories named after extensions
- Duplicate detection uses MD5 or SHA256 hash

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Empty directory | Display "No files found" |
| Permission denied | Display error with path |
| No duplicates found | Display "No duplicates found" |
| Invalid path | Display "Invalid path" error |

---

## Feature 3: Claude Code Integration

### Objective

Install and configure Claude Code CLI for development assistance.

### Detailed Description

Simplifies the installation and configuration of Anthropic's Claude Code AI assistant.

### Sub-Features

#### 3.1 Install Claude Code

Install Claude Code via Windows Package Manager (winget).

#### 3.2 Configure MCP Database

Add MCP (Model Context Protocol) database tool configuration.

#### 3.3 Configure Windows Environment

Update Claude settings for Windows environment (shell, permissions).

### Usage Flow

```bash
# Install Claude Code
devmaid claude install

# Check status
devmaid claude status

# Configure MCP database
devmaid claude settings mcp-database

# Configure Windows environment
devmaid claude settings win-env
```

### Business Rules

- Requires Windows operating system
- Requires winget to be installed
- Installation requires administrator privileges (via UAC)
- Configuration modifies user-level Claude settings

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Winget not installed | Display error with installation instructions |
| Already installed | Skip installation, show status |
| Installation failed | Display error with exit code |
| Configuration file not found | Create new configuration |

---

## Feature 4: OpenCode Integration

### Objective

Install and configure OpenCode CLI tool.

### Detailed Description

Manages OpenCode installation and configuration for development workflows.

### Sub-Features

#### 4.1 Install OpenCode

Install OpenCode via available package managers.

#### 4.2 Check Status

Verify OpenCode installation and version.

#### 4.3 Configure

Set up OpenCode configuration.

### Usage Flow

```bash
# Install OpenCode
devmaid opencode install

# Check status
devmaid opencode status

# Configure
devmaid opencode config
```

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Already installed | Show version information |
| Installation failed | Display error message |
| Not found in PATH | Suggest PATH update |

---

## Feature 5: Winget Package Manager

### Objective

Backup and restore Windows packages installed via winget.

### Detailed Description

Enables users to export their installed packages to a JSON file and restore them on different machines or after system reinstallation.

### Sub-Features

#### 5.1 Backup Packages

Export all installed winget packages to a JSON file.

#### 5.2 Restore Packages

Import packages from a previously created backup.

### Usage Flow

```bash
# Backup packages
devmaid winget backup -o "C:\backups"

# Restore packages
devmaid winget restore -i "C:\backups\backup-winget.json"
```

### Business Rules

- Backup creates `backup-winget.json` in the specified directory
- Restore uses winget import functionality
- Only user-installed packages are backed up (not system packages)
- Restore may require user confirmation for package installation

### Output Format (backup-winget.json)

```json
{
  "CreationDate": "2024-01-15T10:30:00",
  "Packages": [
    {
      "Id": "Git.Git",
      "Version": "2.43.0"
    },
    {
      "Id": "Microsoft.VisualStudioCode",
      "Version": "1.85.0"
    }
  ]
}
```

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| No packages installed | Create empty backup file |
| Backup file already exists | Prompt for overwrite confirmation |
| Restore file not found | Display "File not found" error |
| Package not available | Skip package, continue with others |
| Network unavailable | Display network error, allow retry |

---

## Feature 6: Interactive TUI Mode

### Objective

Provide a user-friendly terminal interface for navigating and executing DevMaid commands.

### Detailed Description

The TUI mode offers an interactive menu-driven interface that makes DevMaid accessible to users who prefer not to remember CLI commands.

### Usage Flow

```bash
devmaid tui
```

1. Main menu is displayed with available commands
2. User navigates using arrow keys
3. User selects a menu item with Enter
4. Sub-menu or command execution occurs
5. Progress is shown in real-time
6. Output dialog displays results
7. User returns to menu or exits

### Menu Structure

```
DevMaid - Terminal User Interface
├── Table Parser
│   ├── Parse CSV to Markdown
│   ├── Parse CSV to JSON
│   ├── Parse Markdown to CSV
│   └── Back
├── File Utils
│   ├── Search Files
│   ├── Organize by Extension
│   ├── Find Duplicates
│   └── Back
├── Claude Code
│   ├── Install Claude Code
│   ├── Check Status
│   ├── Configure
│   └── Back
├── OpenCode
│   ├── Install OpenCode
│   ├── Check Status
│   ├── Configure
│   └── Back
├── Winget
│   ├── Backup Packages
│   ├── Restore Packages
│   └── Back
└── Exit
```

### Keyboard Navigation

| Key | Action |
|-----|--------|
| ↑ / ↓ | Navigate menu items |
| Enter | Execute selected item |
| Esc | Go back / Exit |

### Theme Support

The TUI automatically detects terminal theme:
- **Dark terminal**: Black background, white/gray text
- **Light terminal**: White background, black text

### Real-Time Output

Commands execute asynchronously with real-time output display:
- Progress dialog shows during execution
- Output streams to dialog as received
- Exit code displayed upon completion

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Terminal too small | Show minimum size warning |
| Command not found | Display error in output dialog |
| Command fails | Show error message with exit code |
| Long-running command | Show progress, allow cancellation |

---

## Main Use Cases

### Use Case 1: New Developer Setup

**Scenario:** Developer gets a new Windows machine and wants to set up their development environment.

**Flow:**
1. Install DevMaid via dotnet tool
2. Run `devmaid tui`
3. Use Winget Backup to restore packages from old machine
4. Install Claude Code via menu
5. Install OpenCode via menu

### Use Case 2: Database Class Generation

**Scenario:** Developer needs to create C# classes for existing database tables.

**Flow:**
1. Run `devmaid table-parser -d mydb -t users`
2. Copy generated class to project
3. Modify as needed

### Use Case 3: System Backup

**Scenario:** Developer wants to backup installed applications before system reinstall.

**Flow:**
1. Run `devmaid winget backup -o D:\backups`
2. Store backup file in safe location

---

## Future Roadmap Ideas

### Priority 1 - Near Term

- [ ] Add support for MySQL/SQL Server in Table Parser
- [ ] Add file preview in TUI
- [ ] Add command history in TUI
- [ ] Add configuration file for default options

### Priority 2 - Medium Term

- [ ] Add plugin system for custom commands
- [ ] Add cloud sync for winget backups
- [ ] Add macOS/Linux support
- [ ] Add configuration web interface

### Priority 3 - Long Term

- [ ] Add AI-powered command suggestions
- [ ] Add team collaboration features
- [ ] Add custom script execution
- [ ] Add integration with IDEs (VS Code, Visual Studio)

---

## Appendix: Command Reference

### Quick Reference

| Command | Shortcut | Description |
|---------|----------|-------------|
| `devmaid table-parser` | `tableparser` | Parse table to C# class |
| `devmaid file` | - | File utilities |
| `devmaid file search` | - | Search files |
| `devmaid file organize` | - | Organize by extension |
| `devmaid file duplicates` | - | Find duplicates |
| `devmaid claude` | - | Claude Code commands |
| `devmaid claude install` | - | Install Claude Code |
| `devmaid claude status` | - | Check Claude status |
| `devmaid claude config` | - | Configure Claude |
| `devmaid opencode` | - | OpenCode commands |
| `devmaid winget` | - | Winget commands |
| `devmaid winget backup` | - | Backup packages |
| `devmaid winget restore` | - | Restore packages |
| `devmaid tui` | - | Launch TUI |

---

## Glossary

| Term | Definition |
|------|------------|
| CLI | Command Line Interface |
| TUI | Terminal User Interface |
| Winget | Windows Package Manager |
| MCP | Model Context Protocol |
| DTO | Data Transfer Object |
| PostgreSQL | Open-source relational database |
