# Feature Spec: Table Parser

**ID:** 001  
**Slug:** table-parser  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Enable developers to instantly generate a typed C# class from an existing PostgreSQL table schema, eliminating manual translation of column definitions into property declarations.

---

## User Stories

**US-001.1** — As a developer, I want to connect to a PostgreSQL database and generate a C# class from a table schema, so that I don't have to manually write boilerplate model code.

**US-001.2** — As a developer, I want nullable columns to produce nullable C# types (`int?`, `string?`), so that the generated class accurately reflects the database schema.

**US-001.3** — As a developer, I want to choose where the output file is saved, so that I can drop it directly into my project directory.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-001.1 | Given valid connection parameters and an existing table, the tool generates a `.cs` file with a class whose name matches the table name (PascalCase). |
| AC-001.2 | Each column becomes a public property with the correct C# type mapping (see Business Rules). |
| AC-001.3 | Nullable columns (`IS NULLABLE = YES`) generate nullable property types (`int?`, `bool?`, etc.). |
| AC-001.4 | When `--output` is omitted, the file is written as `./table.class` in the current working directory. |
| AC-001.5 | When the table does not exist, the tool exits with code `1` and prints a specific error message. |
| AC-001.6 | When the password is not provided via `--password`, the tool prompts interactively and does not echo input. |
| AC-001.7 | Unsupported PostgreSQL types generate an `object` property with a `// [UNSUPPORTED TYPE: <pg_type>]` comment. |

---

## CLI Interface

```bash
devmaid table-parser [options]
```

### Options

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `--database` | `-d` | Yes | — | Database name |
| `--table` | `-t` | No | — | Table name to parse |
| `--user` | `-u` | No | `postgres` | Database username |
| `--password` | `-p` | No | prompt | Database password |
| `--host` | `-H` | No | `localhost` | Database host |
| `--port` | — | No | `5432` | Database port |
| `--output` | `-o` | No | `./table.class` | Output file path |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Class file generated successfully |
| `1` | Connection error, table not found, or write error |
| `2` | Missing required option (`--database`) |
| `3` | psql/Npgsql driver error |

---

## Type Mapping

| PostgreSQL Type | C# Type |
|----------------|---------|
| `int`, `integer`, `int4` | `int` |
| `bigint`, `int8` | `long` |
| `smallint`, `int2` | `short` |
| `varchar`, `character varying`, `text` | `string` |
| `boolean`, `bool` | `bool` |
| `numeric`, `decimal` | `decimal` |
| `real`, `float4` | `float` |
| `double precision`, `float8` | `double` |
| `timestamp`, `timestamp without time zone` | `DateTime` |
| `timestamp with time zone`, `timestamptz` | `DateTimeOffset` |
| `date` | `DateOnly` |
| `time` | `TimeOnly` |
| `uuid` | `Guid` |
| `bytea` | `byte[]` |
| `json`, `jsonb` | `string` |
| Any other | `object` + warning comment |

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| Invalid credentials | Exit `1`, message: `"Authentication failed for user '<user>'@'<host>'. Check your credentials."` |
| Table not found | Exit `1`, message: `"Table '<table>' not found in database '<database>'."` |
| Database not found | Exit `1`, message: `"Database '<database>' does not exist on host '<host>'."` |
| Connection timeout | Exit `1`, message: `"Connection to '<host>:<port>' timed out. Is PostgreSQL running?"` |
| Output path invalid | Exit `1`, message: `"Cannot write to path '<path>'. Check permissions."` |
| Empty table (no columns) | Generate empty class with comment `// Table has no columns.` |

---

## Non-Functional Requirements

- Generation must complete in under **2 seconds** for tables with up to 100 columns on a local connection.
- The generated file must be valid, compilable C# (barring any `object` fallback types).
