# Feature Specification Documentation

## Product Overview

FurLab is a .NET-based CLI tool designed to automate common development tasks. It provides a unified interface for database operations, file management, AI tool installation, and Windows package management.

## Feature List

### Core Features

2. **Database Utilities**
3. **File Utilities (Combine)**
4. **Claude Code Integration**
5. **OpenCode Integration**
6. **Winget Package Manager**
7. **SQL Query & CSV Exportation**
8. **Project Cleaner (.NET Clean)**
9. **Windows Features Manager**

---


### Objective

Parse PostgreSQL database tables and automatically generate C# class definitions with properties matching table columns.

### Detailed Description


### Usage Flow

```bash
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

## Feature 2: Database Utilities

### Objective

Provide utilities for backing up and restoring PostgreSQL databases.

### Detailed Description

The database command provides functionality to create backups and restore PostgreSQL databases using pg_dump and pg_restore.

### Sub-Features

#### 2.1 Database Backup

Creates backups of PostgreSQL databases using pg_dump.

#### 2.2 Database Restore

Restores PostgreSQL databases using pg_restore from .dump files.

### Usage Flow

```bash
# Backup a single database
FurLab database backup mydb -h localhost -U postgres

# Backup all databases
FurLab database backup --all -h localhost -U postgres -o "C:\backups"

# Restore a specific database
FurLab database restore mydb "C:\backups\mydb.dump"

# Restore all databases from a directory
FurLab database restore --all "C:\backups"

# Restore all databases from current directory
FurLab database restore --all
```

### Business Rules

- **Backup**:
  - Uses pg_dump to create custom format backups
  - Supports single database or all databases backup
  - Backup files are created with .dump extension
  - Uses current directory for output if not specified

- **Restore**:
  - Uses pg_restore to restore backups
  - Automatically creates database if it doesn't exist
  - Supports single database or all databases restore
  - Searches for .dump files in current directory if not specified
  - Uses filename (without extension) as database name

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| pg_dump/pg_restore not found | Display error with PostgreSQL installation instructions |
| Invalid credentials | Display error message, exit with code 1 |
| Dump file not found | Display "File not found" error |
| Database already exists on restore | Display warning, continue with restore |
| Restore directory not found | Display "Directory not found" error |
| No .dump files found | Display warning "No .dump files found" |

### Options

#### Backup

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `<database>` | No* | - | Database name (required without --all) |
| `-a`, `--all` | No | false | Backup all databases |
| `-h`, `--host` | No | localhost | Database host |
| `-p`, `--port` | No | 5432 | Database port |
| `-U`, `--username` | No | - | Database username |
| `-W`, `--password` | No | - | Password (prompted if not provided) |
| `-o`, `--output` | No | current directory | Output path |

#### Restore

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `<database>` | No* | - | Database name (required without --all) |
| `<file>` | No | `<database>.dump` | Dump file to restore |
| `-a`, `--all` | No | false | Restore all databases |
| `-d`, `--directory` | No | current directory | Directory with .dump files |
| `-h`, `--host` | No | localhost | Database host |
| `-p`, `--port` | No | 5432 | Database port |
| `-U`, `--username` | No | - | Database username |
| `-W`, `--password` | No | - | Password (prompted if not provided) |

---

## Feature 3: File Utilities (Combine)

### Objective

Combine multiple files into a single output file.

### Detailed Description

The Combine feature takes multiple input files matching a pattern and combines them into a single output file. This is useful for consolidating SQL files files, or any, log text-based files.

### Usage Flow

```bash
# Combine all SQL files in a directory
FurLab file combine -i "C:\temp\*.sql" -o "C:\temp\result.sql"

# Combine with default output name
FurLab file combine -i "C:\temp\*.txt"
```

### Business Rules

- Input must be a valid file pattern (e.g., `*.sql`, `*.txt`)
- Output file is created or overwritten
- If no output is specified, creates `CombineFiles.<extension>` in the same directory
- Files are processed in alphabetical order
- UTF-8 encoding is used for output

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| No files match pattern | Throw "Files not Found" error |
| Invalid pattern | Throw "Input pattern is invalid" error |
| Empty pattern | Throw "Input pattern is required" error |

---

## Feature 4: Claude Code Integration

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
FurLab claude install

# Check status
FurLab claude status

# Configure MCP database
FurLab claude settings mcp-database

# Configure Windows environment
FurLab claude settings win-env
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

## Feature 5: OpenCode Integration

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
FurLab opencode install

# Check status
FurLab opencode status

# Configure
FurLab opencode config
```

### Edge Cases and Error Handling

| Scenario | Handling |
|----------|----------|
| Already installed | Show version information |
| Installation failed | Display error message |
| Not found in PATH | Suggest PATH update |

---

## Feature 6: Winget Package Manager

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
FurLab winget backup -o "C:\backups"

# Restore packages
FurLab winget restore -i "C:\backups\backup-winget.json"
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

## Feature 7: SQL Query & CSV Exportation

### Objective
Execute SQL commands against a single database, multiple databases on a single server, or even across multiple servers defined in appsettings.json, automatically exporting the data to `.csv`.

### Detailed Description
Provides high flexibility for querying data with automated output generation supporting local, multi-database, and multi-server distribution modes for massive reporting routines.

### Usage Flow
```bash
FurLab query run --input script.sql --output result.csv -h localhost -d mydb
FurLab query run --all --input script.sql --output ./results
```

---

## Feature 8: Project Cleaner (.NET Clean)

### Objective
Liberate disk space and resolve `.NET` build cache issues by fully deleting any `bin` and `obj` output directories found recursively.

### Usage Flow
```bash
# Clean recursively starting from the current directory:
FurLab clean

# Clean a specific solution folder
FurLab clean "C:\MyProjects"
```

---

## Feature 9: Windows Features Manager

### Objective
Export your activated Windows Optional Features to a JSON backup file using dism, and subsequently import/enable them on new environments.

### Usage Flow
```bash
FurLab windowsfeatures list --enabled-only
FurLab windowsfeatures export "C:\backups\windowsfeatures.json"
FurLab windowsfeatures import "C:\backups\windowsfeatures.json"
```

---

## Main Use Cases

### Use Case 1: New Developer Setup

**Scenario:** Developer gets a new Windows machine and wants to set up their development environment.

**Flow:**
1. Install FurLab via dotnet tool
2. Run `FurLab winget restore` to restore packages from old machine
3. Install Claude Code: `FurLab claude install`
4. Install OpenCode: `FurLab opencode install`

### Use Case 2: Database Class Generation

**Scenario:** Developer needs to create C# classes for existing database tables.

**Flow:**
2. Copy generated class to project
3. Modify as needed

### Use Case 3: System Backup

**Scenario:** Developer wants to backup installed applications before system reinstall.

**Flow:**
1. Run `FurLab winget backup -o D:\backups`
2. Store backup file in safe location

---

## Future Roadmap Ideas

### Priority 1 - Near Term

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
| `FurLab file` | - | File utilities |
| `FurLab file combine` | - | Combine files into one |
| `FurLab claude` | - | Claude Code commands |
| `FurLab claude install` | - | Install Claude Code |
| `FurLab claude status` | - | Check Claude status |
| `FurLab claude config` | - | Configure Claude |
| `FurLab opencode` | - | OpenCode commands |
| `FurLab winget` | - | Winget commands |
| `FurLab winget backup` | - | Backup packages |
| `FurLab winget restore` | - | Restore packages |
| `FurLab database` | - | Database commands |
| `FurLab database backup` | - | Backup database |
| `FurLab database restore` | - | Restore database |
| `FurLab query run` | - | Run multi-database query and export CSV |
| `FurLab clean` | - | Recursively clean bin/obj output folders |
| `FurLab windowsfeatures` | - | Manage Windows Optional Features |

---

## Glossary

| Term | Definition |
|------|------------|
| CLI | Command Line Interface |
| Winget | Windows Package Manager |
| MCP | Model Context Protocol |
| DTO | Data Transfer Object |
| PostgreSQL | Open-source relational database |
