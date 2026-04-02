# Feature Spec: Database Utilities

**ID:** 002  
**Slug:** database-utilities  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Provide developers with a simple, unified way to back up and restore PostgreSQL databases using the standard `pg_dump` / `pg_restore` toolchain, supporting both single-database and bulk operations.

---

## User Stories

**US-002.1** — As a developer, I want to create a binary backup of a single PostgreSQL database, so that I can restore it later or move it to another environment.

**US-002.2** — As a developer, I want to back up all databases on a server with a single command, so that I can create full environment snapshots efficiently.

**US-002.3** — As a developer, I want to restore a database from a `.dump` file, so that I can recover data or replicate environments.

**US-002.4** — As a developer, I want to restore all `.dump` files from a directory, so that I can reconstruct an entire environment from a backup set.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-002.1 | Running `devmaid database backup <db>` creates `<db>.dump` in the current directory using `pg_dump --format=custom`. |
| AC-002.2 | Running `devmaid database backup --all` creates one `.dump` file per database found on the server (excluding `template0`, `template1`, `postgres` unless explicitly included). |
| AC-002.3 | When `--output` is specified for backup, the dump file(s) are created in that directory. |
| AC-002.4 | Running `devmaid database restore <db> <file>` runs `pg_restore` against the specified database and file. |
| AC-002.5 | If the target database does not exist during restore, it is created automatically before `pg_restore` is invoked. |
| AC-002.6 | Running `devmaid database restore --all` restores every `.dump` file found in the current directory (or `--directory` if specified). |
| AC-002.7 | The database name is inferred from the dump file's base name (without extension) when using `--all`. |
| AC-002.8 | Passwords are prompted interactively if not provided via `--password`. |

---

## CLI Interface

### Backup

```bash
devmaid database backup [<database>] [options]
devmaid database backup --all [options]
```

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `<database>` | — | Yes (without `--all`) | — | Database to back up |
| `--all` | `-a` | No | `false` | Back up all databases |
| `--host` | `-h` | No | `localhost` | Database host |
| `--port` | `-p` | No | `5432` | Database port |
| `--username` | `-U` | No | — | Username |
| `--password` | `-W` | No | prompt | Password |
| `--output` | `-o` | No | `./` | Output directory |

### Restore

```bash
devmaid database restore [<database> [<file>]] [options]
devmaid database restore --all [options]
```

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `<database>` | — | Yes (without `--all`) | — | Target database |
| `<file>` | — | No | `<database>.dump` | Dump file path |
| `--all` | `-a` | No | `false` | Restore all `.dump` files |
| `--directory` | `-d` | No | `./` | Directory with `.dump` files |
| `--host` | `-h` | No | `localhost` | Database host |
| `--port` | `-p` | No | `5432` | Database port |
| `--username` | `-U` | No | — | Username |
| `--password` | `-W` | No | prompt | Password |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Operation completed successfully |
| `1` | Database or file error |
| `2` | Missing required argument |
| `3` | `pg_dump` or `pg_restore` not found in PATH |

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| `pg_dump` not found | Exit `3`, message with installation instructions |
| `pg_restore` not found | Exit `3`, message with installation instructions |
| Invalid credentials | Exit `1`, specific auth error message |
| Dump file not found | Exit `1`, `"File '<path>' not found."` |
| Output directory not found | Exit `1`, `"Output directory '<path>' does not exist."` |
| No `.dump` files in directory | Exit `0` with warning: `"No .dump files found in '<directory>'."` |
| Database already exists on restore | Log warning, proceed with restore (do not abort) |

---

## Non-Functional Requirements

- Progress must be printed to stdout during both backup and restore for operations exceeding 1 second.
- Each database operation (backup or restore) must complete independently; a failure on one database in `--all` mode must not abort the remaining databases.
- The final summary must report: total attempted, successful, failed.
