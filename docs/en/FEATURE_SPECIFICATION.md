# Feature Specification Documentation

## Product Overview

FurLab is a .NET-based CLI tool designed to automate common development tasks. It provides a unified interface for database operations, file management, AI tool installation, and Windows package management.

## Feature List

### Core Features

1. **Database Utilities** (Backup, Restore, PgPass)
2. **File Utilities** (Combine)
3. **Claude Code Integration**
4. **OpenCode Integration**
5. **Winget Package Manager**
6. **SQL Query & Export** (Multi-server & Multi-database)
7. **Docker Utilities** (Postgres)
8. **Windows Features Manager**
9. **Settings Management** (Database Servers)

---

## Feature 1: Database Utilities

### Objective

Provide utilities for backup, restore, and credential management for PostgreSQL databases.

### Detailed Description

The `database` command provides functionality to create backups and restore PostgreSQL databases using `pg_dump` and `pg_restore`, as well as managing the `pgpass.conf` file.

### Sub-Features

#### 1.1 Database Backup

Creates backups of PostgreSQL databases using `pg_dump`.

#### 1.2 Database Restore

Restores PostgreSQL databases using `pg_restore` from `.dump` files.

#### 1.3 PgPass Management

Manages the `pgpass.conf` file to allow passwordless connections. Supports the use of wildcards (`*`) for host, port, and user.

### Usage Flow

```bash
# Backup a single database
fur database backup mydb -H localhost -U postgres

# Backup all databases
fur database backup --all -H localhost -U postgres -o "C:\backups"

# Restore a specific database
fur database restore mydb "C:\backups\mydb.dump"

# Add entry to pgpass
fur database pgpass add mydb --host localhost --username postgres --password mypassword

# List pgpass entries
fur database pgpass list
```

---

## Feature 2: File Utilities (Combine & Convert)

### Objective

Provide utilities for manipulating text files, such as combining and converting encoding.

### Detailed Description

The `file` command provides tools for batch file processing, supporting glob patterns for file selection.

### Sub-Features

#### 2.1 Combine Files (Combine)

Takes multiple input files matching a glob pattern and combines them into a single output file.

#### 2.2 Convert Encoding (ConvertEncoding)

Converts text files between different encodings (e.g., Latin1 to UTF-8). Features automatic source encoding detection, backup support, and preservation of file metadata.

### Usage Flow

```bash
# Combine all SQL files in a directory
fur file combine -i "C:\temp\*.sql" -o "C:\temp\result.sql"

# Convert all .cs files to UTF-8 (auto-detection)
fur file convert-encoding -i "**/*.cs" --to UTF-8

# Convert files with backup and text-only filtering
fur file convert-encoding -i "docs/*" --from Windows-1252 --to UTF-8 --backup --text-only
```

---

## Feature 3: Claude Code Integration

### Objective

Install and configure the Claude Code CLI.

### Usage Flow

```bash
# Install Claude Code
fur claude install

# Configure MCP database settings
fur claude settings mcp-database

# Configure Windows environment
fur claude settings win-env
```

---

## Feature 4: OpenCode Integration

### Objective

Configure the OpenCode CLI tool.

### Usage Flow

```bash
# Configure MCP database settings in OpenCode
fur opencode settings mcp-database

# Set default model for OpenCode
fur opencode settings default-model claude-3-5-sonnet-20241022 --global
```

---

## Feature 5: Winget Package Manager

### Objective

Backup and restore Windows packages installed via winget.

### Usage Flow

```bash
# Package backup
fur winget backup -o "C:\backups"

# Restore packages
fur winget restore -i "C:\backups\winget-import.json"
```

---

## Feature 6: SQL Query & Export

### Objective
Execute SQL scripts on one or more databases/servers and export to CSV.

### Detailed Description
Supports parallel execution across multiple servers configured in `settings db-servers`.

### Usage Flow
```bash
# Execute on a specific database
fur query run -f script.sql -d mydb -H localhost

# Execute on ALL configured servers
fur query run -f script.sql --all
```

---

## Feature 7: Docker Utilities

### Objective
Manage Docker containers useful for development.

### Sub-Features
- **Postgres**: Starts a local PostgreSQL container with default settings.

### Usage Flow
```bash
fur docker postgres
```

---

## Feature 8: Windows Features Manager

### Objective
Backup and restore Windows optional Features.

### Usage Flow
```bash
fur windowsfeatures list
fur windowsfeatures export -o "C:\backups\features.json"
fur windowsfeatures import -i "C:\backups\features.json"
```

---

## Feature 9: Settings Management

### Objective
Manage database servers configured for global use in FurLab.

### Detailed Description
Allows registering servers with encrypted credentials to facilitate use in `query` and `database` commands.

### Usage Flow
```bash
# List servers
fur settings db-servers ls

# Add server
fur settings db-servers add PROD --host 10.0.0.1 --username admin --database main

# Set password (encrypted)
fur settings db-servers set-password PROD

# Test connection
fur settings db-servers test PROD
```

---

## Appendix: Command Reference

| Command | Description |
|---------|-------------|
| `fur database` | PostgreSQL utilities (backup, restore, pgpass) |
| `fur file` | File utilities (combine, convert-encoding) |
| `fur claude` | Claude Code installation and configuration |
| `fur opencode` | OpenCode configuration |
| `fur winget` | Winget package backup and restore |
| `fur query run` | Multi-server SQL execution with CSV export |
| `fur docker` | Docker utilities (postgres) |
| `fur windowsfeatures` | Manage Windows features (dism) |
| `fur settings db-servers` | Manage registered database servers |
