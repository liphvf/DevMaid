# FurLab API Endpoints

The FurLab API provides local-only HTTP endpoints for executing SQL queries across PostgreSQL servers with real-time progress streaming via Server-Sent Events (SSE).

> **Security Note**: The API only accepts connections from `localhost` (`127.0.0.1` / `::1`). Requests from external IPs are rejected with HTTP 403.

## Base URL

```
http://localhost:5000
```

## Endpoints

### POST /api/query/analyze

Analyzes a SQL query and returns its type, destructiveness, and estimated impact.

**Request Body:**
```json
{
  "sql": "SELECT * FROM users",
  "servers": ["prod-1", "prod-2"],
  "allDatabases": false,
  "exclude": "template0,template1"
}
```

**Response:**
```json
{
  "queryType": "SELECT",
  "isDestructive": false,
  "affectedServers": 2,
  "estimatedDatabases": 4,
  "requiresConfirmation": false
}
```

### POST /api/query/execute

Initiates query execution across configured servers.

**Request Body:**
```json
{
  "sql": "SELECT * FROM users",
  "servers": ["prod-1"],
  "allDatabases": false,
  "exclude": null,
  "confirmed": true
}
```

**Response:**
```json
{
  "executionId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "started",
  "outputDirectory": "C:/Users/.../results/2026-05-04_143022"
}
```

> For destructive queries, call `/analyze` first, then set `confirmed: true`.

### GET /api/query/status?executionId={id}

Returns the current status of an execution. Useful for reconnection.

**Response:**
```json
{
  "executionId": "550e8400-...",
  "status": "running",
  "totalDatabases": 5,
  "completed": 2,
  "failed": 0,
  "inProgress": 3,
  "startedAt": "2026-05-04T14:30:22Z",
  "completedAt": null,
  "outputDirectory": "C:/Users/.../results/2026-05-04_143022",
  "results": [
    {
      "server": "prod-1",
      "database": "app_db",
      "status": "Success",
      "rowCount": 150,
      "durationMs": 320,
      "error": null
    }
  ]
}
```

### GET /api/query/events?executionId={id}

Streams Server-Sent Events (SSE) with real-time progress updates.

**Event Types:**

| Event | Description |
|-------|-------------|
| `execution-started` | Execution began |
| `query-executing` | Query started on a specific database |
| `query-completed` | Query finished successfully (includes preview of up to 50 rows) |
| `query-failed` | Query failed on a database |
| `execution-completed` | All queries finished |

**Example SSE Stream:**
```
event: execution-started
data: {"executionId":"...","servers":1,"databases":5,"timestamp":"..."}

event: query-completed
data: {"server":"prod-1","database":"app_db","rowCount":150,"preview":[...]}

event: execution-completed
data: {"successCount":5,"failureCount":0,"totalRows":750,"outputDirectory":"..."}
```

### POST /api/query/cancel?executionId={id}

Cancels a running execution.

**Response:**
```json
{
  "executionId": "550e8400-...",
  "status": "cancelled"
}
```

### GET /api/query/download?executionId={id}&type={type}

Downloads the result CSV file.

**Query Parameters:**
- `type`: `consolidated` (default) or `log`

**Response:** `text/csv` file download

## Configuration

The API reuses the same `settings.json` as the CLI (`%LocalAppData%/FurLab/settings.json`). Server configurations, credentials, and default output directory are shared.

## Running the API

```bash
dotnet run --project FurLab.Api
```

The API will start on `http://localhost:5000`.
