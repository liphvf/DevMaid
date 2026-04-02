# Feature Spec: SQL Query & CSV Export

**ID:** 007  
**Slug:** sql-query-csv-export  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Enable developers and data engineers to execute SQL queries against PostgreSQL databases and export results to CSV — supporting single databases, all databases on a server, and multiple servers configured in `appsettings.json` — for reporting, auditing, and data extraction workflows.

---

## User Stories

**US-007.1** — As a developer, I want to run a SQL script against a specific database and save the results as a CSV file, so that I can share query results with non-technical stakeholders.

**US-007.2** — As a DBA, I want to run the same query across all databases on a server, so that I can collect metrics or audit data across the entire server in one operation.

**US-007.3** — As a DBA, I want to run a query across multiple configured servers simultaneously, so that I can produce cross-environment reports without manual repetition.

**US-007.4** — As a developer, I want to filter which servers are included using a wildcard pattern, so that I can target only production or only staging servers.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-007.1 | `devmaid query run --input <sql> --output <csv>` executes the SQL and writes results to the specified CSV file with a header row. |
| AC-007.2 | `devmaid query run --all --input <sql> --output <dir>` creates `all_databases.csv` in `<dir>` with a `_database_name` prefix column. |
| AC-007.3 | `devmaid query run --all --separate-files --input <sql> --output <dir>` creates one `<database>.csv` file per database in `<dir>`. |
| AC-007.4 | `devmaid query run --servers --input <sql> --output <dir>` executes against all servers in `appsettings.json` with `Servers.Enabled = true`, creating `<dir>/<server>/<database>.csv` for each result. |
| AC-007.5 | `--server-filter <pattern>` limits server execution to servers whose `Name` matches the wildcard pattern (case-insensitive, `*` supported). |
| AC-007.6 | `--exclude <list>` skips listed database names (comma-separated) when using `--all`. |
| AC-007.7 | Connection parameters from CLI override those in `appsettings.json`. |
| AC-007.8 | NULL values in query results appear as empty fields in the CSV. |
| AC-007.9 | Fields containing commas or quotes are properly escaped per RFC 4180. |
| AC-007.10 | Progress is printed per database and per server during multi-target operations. |
| AC-007.11 | A summary is printed at the end of multi-target operations: total databases, successful, failed, total rows. |

---

## CLI Interface

```bash
# Single database
devmaid query run --input <sql> --output <file> [connection options]

# All databases on a server
devmaid query run --all --input <sql> --output <directory> [options]

# All configured servers
devmaid query run --servers --input <sql> --output <directory> [options]
```

### Options

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `--input` | `-i` | Yes | — | Path to SQL file |
| `--output` | `-o` | Yes | — | Output CSV file (single) or directory (multi) |
| `--all` | `-a` | No | `false` | Execute across all databases on server |
| `--separate-files` | — | No | `false` | One CSV per database (requires `--all`) |
| `--exclude` | — | No | — | Comma-separated database names to skip |
| `--servers` | `-s` | No | `false` | Execute across all configured servers |
| `--server-filter` | — | No | — | Wildcard filter for server names |
| `--host` | `-h` | No | from config | Database host |
| `--port` | `-p` | No | `5432` | Database port |
| `--database` | `-d` | No (with `--all`) | from config | Target database |
| `--username` | `-U` | No | from config | Username |
| `--password` | `-W` | No | prompt | Password |
| `--ssl-mode` | — | No | `Prefer` | SSL mode |
| `--timeout` | — | No | `30` | Connection timeout (seconds) |
| `--command-timeout` | — | No | `300` | Query execution timeout (seconds) |
| `--npgsql-connection-string` | — | No | — | Full Npgsql connection string (overrides individual params) |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | All queries completed successfully |
| `1` | One or more queries failed (partial success is still exit `1`) |
| `2` | Missing required option |
| `3` | psql not found (required for `--all`) |

---

## Multi-Server Configuration

Servers are defined in `appsettings.json`:

```json
{
  "Servers": {
    "Enabled": true,
    "PrimaryServer": "dev-local",
    "ServersList": [
      {
        "Name": "dev-local",
        "Host": "localhost",
        "Port": "5432",
        "Username": "postgres",
        "Password": "",
        "Database": "mydb",
        "Databases": [],
        "SslMode": "Prefer",
        "Timeout": 30,
        "CommandTimeout": 300
      }
    ]
  }
}
```

### Database Resolution Order (per server)

1. Server's `Databases` list (if non-empty)
2. `--all` flag → list all databases via psql (applying `--exclude`)
3. Server's `Database` default field

### Connection Parameter Precedence (highest first)

1. CLI flags (`--host`, `--port`, etc.)
2. Server-specific configuration in `ServersList`
3. `PrimaryServer` configuration
4. Defaults

---

## Output Structure

### Single Database

```
result.csv                    ← header + rows
```

### Multi-Database (Consolidated)

```
results/
└── all_databases.csv         ← _database_name + all columns
```

### Multi-Database (Separate Files)

```
results/
├── app_prod.csv
├── app_dev.csv
└── app_test.csv
```

### Multi-Server

```
results/
├── prod-primary/
│   ├── app_prod.csv
│   └── analytics.csv
└── staging/
    └── app_staging.csv
```

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| SQL file not found | Exit `1`, message: `"Input file '<path>' not found."` |
| SQL syntax error | Exit `1`, print PostgreSQL error message |
| Connection failed | Log failure for that database, continue with others in multi-mode |
| psql not found (for `--all`) | Exit `3`, message with PostgreSQL installation instructions |
| No servers match `--server-filter` | Exit `1`, message: `"No servers found matching pattern '<pattern>'."` |
| `Servers.Enabled = false` with `--servers` | Exit `1`, message: `"Multi-server configuration is not enabled in appsettings.json."` |
| `PrimaryServer` not configured | Exit `1`, message with correction instructions |

---

## Non-Functional Requirements

- Must handle result sets of up to **1 million rows** without holding all rows in memory (streaming write).
- Multi-server execution must process servers **sequentially** (not in parallel) to avoid overwhelming the network or credentials management.
- Progress output must not interfere with the CSV output file.
