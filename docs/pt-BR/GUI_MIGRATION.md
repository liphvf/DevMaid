# FurLab Desktop GUI — Plano de Migração (Tauri 2 + Angular 21 + .NET)

> **Status:** Decisões arquiteturais finalizadas em sessão de exploração.
> **Escopo:** MVP inicial focado exclusivamente no comando `query run`.
> **Público-alvo:** Futuro implementador (eu mesmo) e agentes de código.

---

## 1. Visão Geral da Arquitetura

A arquitetura proposta substitui o plano anterior (Electron + Angular + ASP.NET Core) por uma stack mais leve e performática, mantendo a separação de responsabilidades já existente no FurLab.

### 1.1. Stack Tecnológica Final

| Camada | Tecnologia | Justificativa |
|--------|-----------|---------------|
| **Frontend** | Angular 21 + Angular Material 21 | Build to learn; Material Design maduro; RxJS para streams reativos |
| **Desktop Shell** | Tauri 2 | Bundle ~5MB vs ~150MB do Electron; WebView nativa do SO; memória mínima |
| **Backend API** | ASP.NET Core (.NET 10) | Reaproveita `FurLab.Core`; Kestrel como sidecar do Tauri |
| **Comunicação** | NDJSON over HTTP (streaming) | Mais eficiente que SSE/EventSource; suporte nativo do ASP.NET Core 10 para `IAsyncEnumerable` |
| **Editor SQL** | CodeMirror 6 | Bundle ~200KB vs ~3-5MB do Monaco; suficiente para SQL |
| **State Management** | RxJS `BehaviorSubject` + services singleton | Dentro do framework Angular; sem NgRx/Akita/Redux |

### 1.2. Diagrama de Arquitetura de Alto Nível

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          FURLAB DESKTOP (TAURI 2)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ Processo Principal: Tauri (Rust)                                      │ │
│  │ • Janela nativa, tray icon, atalhos globais                           │ │
│  │ • Auto-updater (futuro)                                               │ │
│  │ • Gerencia lifecycle do sidecar .NET                                  │ │
│  │ • IPC: window controls, open folder, save dialog                      │ │
│  │                                                                       │ │
│  │ ┌──────────────┐         HTTP localhost         ┌─────────────────┐   │ │
│  │ │  Angular 21  │◄──────────────────────────────►│  FurLab.Api     │   │ │
│  │ │  + Material  │   NDJSON streaming             │  (Kestrel       │   │ │
│  │ │  + RxJS      │   (ReadableStream)             │   sidecar)      │   │ │
│  │ │  + CodeMirror│                                │                 │   │ │
│  │ └──────────────┘                                └────────┬────────┘   │ │
│  │                                                        │            │ │
│  │                                                 ┌──────┴──────┐     │ │
│  │                                                 │ FurLab.Core │     │ │
│  │                                                 │  (.NET 10)  │     │ │
│  │                                                 │  (existente)│     │ │
│  │                                                 └─────────────┘     │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  Modo Web (fur gui --web):                                                  │
│  ┌──────────────┐                                                           │
│  │  Navegador   │──► http://localhost:PORT (mesma API)                       │
│  └──────────────┘                                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.3. Comunicação: Por que NDJSON em vez de SSE ou SignalR

A comunicação entre Angular e .NET API é **unidirecional** (servidor → cliente) para 100% dos fluxos do FurLab. O servidor envia eventos de progresso, resultados e erros; o cliente apenas inicia a requisição e pode cancelá-la.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              NDJSON: NEWLINE DELIMITED JSON                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  O ASP.NET Core 10, ao receber:                                             │
│  GET /api/query/execute                                                     │
│  Accept: application/x-ndjson                                               │
│                                                                             │
│  Retorna um stream contínuo onde CADA LINHA é um objeto JSON completo:      │
│                                                                             │
│  HTTP/1.1 200 OK                                                            │
│  Content-Type: application/x-ndjson                                         │
│  Transfer-Encoding: chunked                                                 │
│                                                                             │
│  {"type":"start","sessionId":"abc","servers":2,"totalDatabases":12}\n       │
│  {"type":"db_discovered","server":"prod-01","databases":["db1","db2"]}\n    │
│  {"type":"progress","server":"prod-01","db":"db1","currentStep":1,"totalSteps":12}\n│
│  {"type":"result","server":"prod-01","db":"db1","status":"success","rows":450}\n│
│  {"type":"complete","successCount":11,"failureCount":1,"totalRows":2340}\n    │
│                                                                             │
│  Vantagens sobre SSE:                                                       │
│  • Sem prefixo "data:" em cada linha — menos bytes                         │
│  • Sem framing SSE — parsing mais simples no cliente                        │
│  • Suporte nativo do ASP.NET Core 10 para IAsyncEnumerable<T>               │
│  • Usa fetch() + ReadableStream — controle total no Angular                 │
│                                                                             │
│  Vantagens sobre SignalR:                                                   │
│  • Sem dependência @microsoft/signalr — menos bundle                        │
│  • Sem WebSocket upgrade — funciona em qualquer proxy corporativo           │
│  • Sem Hub/Groups/Connection management — menos código no servidor          │
│  • Reconexão é simples: basta refazer o fetch                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Estrutura de Pastas e Projetos

```
FurLab/
├── FurLab.Core/                    # Existente — Business Logic (.NET 10)
│   ├── FurLab.Core.csproj
│   └── ...
│
├── FurLab.CLI/                     # Existente — Console Application (.NET 10)
│   ├── FurLab.CLI.csproj           # NÃO consome a API (mantém acesso direto ao Core)
│   └── ...
│
├── FurLab.Api/                     # NOVO — ASP.NET Core Web API (.NET 10)
│   ├── FurLab.Api.csproj           # <ProjectReference> FurLab.Core
│   ├── Program.cs                  # Kestrel, middleware, CORS, localhost-only
│   ├── Controllers/
│   │   ├── QueryController.cs      # GET /api/query/execute (NDJSON streaming)
│   │   ├── ServersController.cs    # GET /api/servers
│   │   └── DownloadController.cs   # GET /api/query/download/{sessionId}
│   ├── Middleware/
│   │   └── LocalhostOnlyMiddleware.cs
│   └── appsettings.json
│
├── FurLab.Tests/                   # Existente
│   └── ...
│
├── src-ui/                         # NOVO — Angular 21 Application
│   ├── angular.json
│   ├── package.json
│   ├── src/
│   │   ├── app/
│   │   │   ├── features/
│   │   │   │   └── query/
│   │   │   │       ├── query-run.component.ts          # Orquestrador dos 4 passos
│   │   │   │       ├── query-editor.component.ts         # CodeMirror 6 wrapper
│   │   │   │       ├── server-selector.component.ts      # Seleção multi-servidor
│   │   │   │       ├── query-progress.component.ts       # Progresso + tabela ao vivo
│   │   │   │       └── query-results.component.ts        # Tabela consolidada final
│   │   │   ├── core/
│   │   │   │   ├── services/
│   │   │   │   │   ├── api-url.service.ts                # Descoberta de porta
│   │   │   │   │   ├── query.service.ts                  # Consumo NDJSON + REST
│   │   │   │   │   ├── server.service.ts                 # GET /api/servers
│   │   │   │   │   ├── query-state.service.ts            # BehaviorSubject central
│   │   │   │   │   ├── query-orchestrator.service.ts     # Coordena execução SSE
│   │   │   │   │   └── tauri-shell.service.ts            # IPC do Tauri
│   │   │   │   └── models/
│   │   │   │       └── query-events.ts                   # Interfaces TypeScript
│   │   │   ├── shared/
│   │   │   │   └── ...
│   │   │   └── app.component.ts
│   │   ├── index.html
│   │   └── main.ts
│   └── tsconfig.json
│
├── src-tauri/                      # NOVO — Tauri 2 (Rust)
│   ├── Cargo.toml
│   ├── tauri.conf.json             # Janela, sidecar, ícone, CSP
│   ├── src/
│   │   └── main.rs                 # Spawna sidecar, parse stdout, gerencia lifecycle
│   ├── capabilities/
│   │   └── default.json            # Permissões Tauri
│   └── binaries/
│       └── FurLab-x86_64-pc-windows-msvc.exe   # Sidecar .NET (copiado no build)
│
├── FurLab.slnx                     # ATUALIZADO — inclui FurLab.Api
├── scripts/                        # NOVO
│   ├── build-desktop.ps1           # Orquestra dotnet build → ng build → tauri build
│   ├── dev.ps1                     # Modo desenvolvimento (3 terminais)
│   └── build-api.ps1               # Só a API
├── docs/
│   └── pt-BR/
│       └── GUI_MIGRATION.md        # Este arquivo
└── ...
```

**Nota:** O Tauri não entra no `FurLab.slnx` pois é um projeto Rust/Cargo. O `dotnet build` cuida do backend; o `cargo build` (via `tauri`) cuida do shell desktop.

---

## 3. Segurança: Forçar Acesso Apenas Local

Como a API não requer autenticação (uso local exclusivo), implementamos **defesa em profundidade** em 3 camadas:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SEGURANÇA: API LOCAL APENAS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Camada 1 — Bind apenas em loopback                                         │
│  ─────────────────────────────────                                          │
│  builder.WebHost.UseUrls("http://127.0.0.1:0");                             │
│  ⚠️ NUNCA usar "http://0.0.0.0:0" ou "http://*:0"                           │
│                                                                             │
│  Camada 2 — Middleware de verificação de IP                                 │
│  ────────────────────────────────────────                                   │
│  app.Use(async (context, next) => {                                         │
│      var remoteIp = context.Connection.RemoteIpAddress;                     │
│      if (remoteIp == null || !IPAddress.IsLoopback(remoteIp)) {             │
│          context.Response.StatusCode = 403;                                 │
│          await context.Response.WriteAsync("Forbidden: local only");        │
│          return;                                                            │
│      }                                                                      │
│      await next();                                                          │
│  });                                                                        │
│                                                                             │
│  Camada 3 — Header secreto (deep defense, opcional)                         │
│  ─────────────────────────────────────────────────                          │
│  O Tauri gera um UUID na inicialização, passa via env var para o sidecar    │
│  e expõe para o Angular via invoke(). O Angular envia em TODAS as reqs:     │
│  X-FurLab-Secret: <uuid>                                                    │
│                                                                             │
│  .NET valida:                                                               │
│  var expected = Environment.GetEnvironmentVariable("FURLAB_API_SECRET");    │
│  if (context.Request.Headers["X-FurLab-Secret"] != expected) { ... }        │
│                                                                             │
│  Para o MVP, Camada 1 + 2 são suficientes. Camada 3 é paranóia saudável.    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Descoberta de Porta do Sidecar

O Kestrel usa porta 0 (aleatória) para evitar conflitos. O Tauri descobre a porta via parsing do stdout:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              DESCoberta DE PORTA: FLUXO COMPLETO                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Tauri (Rust)              FurLab.Api.exe (sidecar)                         │
│  ─────────────             ────────────────────────                         │
│                                                                             │
│  spawna com args:                                                          │
│  ["--urls", "http://127.0.0.1:0"]                                          │
│       │                                                                     │
│       │──────────────────────────────────────►                              │
│       │                                    Kestrel escolhe porta livre      │
│       │                                    ex: 52431                        │
│       │                                                                     │
│       │◄──────────────────────────────────────                              │
│       │  stdout: "[FurLab.Api] Listening on http://127.0.0.1:52431"        │
│       │                                                                     │
│  parse com regex:                                                           │
│  r"\[FurLab\.Api\] Listening on (http://[\d\.]+:\d+)"                     │
│       │                                                                     │
│       ▼                                                                     │
│  api_url = "http://127.0.0.1:52431"                                         │
│       │                                                                     │
│       ├──► salva no estado do Tauri                                         │
│       └──► emite evento "api-ready" para o Angular                          │
│                                                                             │
│  Angular:                                                                   │
│  listen("api-ready", (event) => { apiUrl = event.payload; });              │
│  // Todos os HttpClient interceptors usam essa URL como base                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Fallback:** se o parsing de stdout falhar (log noise, etc.), o .NET pode escrever a porta em um arquivo temporário conhecido (`%TEMP%/furlab-api-port.txt`) que o Tauri lê.

---

## 5. API Endpoints — Contrato Completo

### 5.1. Endpoints REST

```
GET   /api/health
      → { "status": "healthy", "version": "1.0.0" }
      → Usado pelo Tauri para verificar se a API subiu corretamente

GET   /api/servers
      → [ { "name": "prod-01", "host": "prod-db-01.company.com", "port": 5432,
            "databases": ["app_1", "app_2"], "fetchAllDatabases": false }, ... ]
      → Fonte: IUserConfigService.GetServers()

POST  /api/query/analyze
      body: { "sql": "SELECT datname FROM pg_database" }
      → { "type": "SELECT", "isDestructive": false, "tables": ["pg_database"],
          "estimatedCost": "low" }
      → Fonte: SqlQueryAnalyzer do Core

POST  /api/query/preview
      body: { "sql": "...", "servers": ["prod-01"], "fetchAllDatabases": true }
      → { "servers": [
          { "name": "prod-01", "databases": ["db1", "db2", "db3"], "excluded": 0 } ] }
      → Faz auto-discovery SEM executar a query

GET   /api/query/execute
      query: ?sql=...&servers=prod-01,prod-02&fetchAll=true
      headers: Accept: application/x-ndjson
      → NDJSON stream de QueryEvent (ver seção 5.2)
      → A JOIA DA COROA — streaming em tempo real

POST  /api/query/cancel/{sessionId}
      → 204 No Content
      → Propaga CancellationTokenSource para parar Parallel.ForEachAsync

GET   /api/query/download/{sessionId}?type=consolidated|errors|log
      → 200 OK, Content-Type: text/csv
      → FileResult do ASP.NET Core
```

### 5.2. Contrato NDJSON — Eventos

Cada linha do stream é um objeto JSON. Interface TypeScript:

```typescript
// src-ui/src/app/core/models/query-events.ts

type QueryEvent =
  | StartEvent
  | DbDiscoveredEvent
  | ProgressEvent
  | ResultEvent
  | ErrorEvent
  | RetryEvent
  | CompleteEvent
  | CancelledEvent;

interface StartEvent {
  type: 'start';
  sessionId: string;
  timestamp: string; // ISO 8601
  servers: number;
  totalDatabases: number;
  sql: string;
  queryType: 'SELECT' | 'INSERT' | 'UPDATE' | 'DELETE' | 'DDL' | 'UNKNOWN';
  isDestructive: boolean;
}

interface DbDiscoveredEvent {
  type: 'db_discovered';
  sessionId: string;
  timestamp: string;
  server: string;
  databases: string[];
}

interface ProgressEvent {
  type: 'progress';
  sessionId: string;
  timestamp: string;
  server: string;
  database: string;
  status: 'connecting' | 'executing' | 'fetching' | 'writing';
  currentStep: number;
  totalSteps: number;
}

interface ResultEvent {
  type: 'result';
  sessionId: string;
  timestamp: string;
  server: string;
  database: string;
  status: 'success' | 'error';
  rows: number;
  durationMs: number;
  columns?: string[];           // apenas no success, para preview
  sampleData?: Record<string, string>[]; // primeiras 5 rows
  error?: string;
}

interface ErrorEvent {
  type: 'error';
  sessionId: string;
  timestamp: string;
  server: string;
  database: string;
  error: string;
  willRetry: boolean;
  retryAttempt?: number;
  maxRetries?: number;
}

interface RetryEvent {
  type: 'retry';
  sessionId: string;
  timestamp: string;
  server: string;
  database: string;
  retryAttempt: number;
  maxRetries: number;
  nextDelayMs: number;
}

interface CompleteEvent {
  type: 'complete';
  sessionId: string;
  timestamp: string;
  servers: number;
  successCount: number;
  failureCount: number;
  totalRows: number;
  totalDurationMs: number;
  outputDirectory: string;
}

interface CancelledEvent {
  type: 'cancelled';
  sessionId: string;
  timestamp: string;
  reason: 'user_request' | 'connection_lost';
  processedCount: number;
}
```

### 5.3. Do Core Channel para NDJSON

O `QueryRunCommand` do Core usa `Channel<CsvRow>` para comunicação paralela. A API atua como adapter:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         ADAPTER: CORE CHANNEL → NDJSON STREAM                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  FurLab.Core                          FurLab.Api                            │
│  ───────────                          ──────────                            │
│                                                                             │
│  Parallel.ForEachAsync escreve       IAsyncEnumerable<QueryEvent>          │
│  no Channel<CsvRow>:                  implementa adapter:                   │
│                                                                             │
│  { Server, DB, Status, Rows,         1. Cria Channel<QueryEvent> interno   │
│    DurationMs, Error }               2. Task.Run executa Core logic        │
│                                      3. Converte CsvRow → QueryEvent        │
│                                      4. yield return cada evento            │
│                                                                             │
│  O ASP.NET Core 10 serializa cada                                       │
│  yield return como uma linha NDJSON.                                      │
│  Zero boilerplate de formatação manual.                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Fluxo de Tela: Query Run (4 Passos)

### Passo 1 — Editor SQL

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🐺 FurLab                                          [—] [□] [×]             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  SELECT datname, pg_database_size(datname)                            │ │
│  │  FROM pg_database                                                     │ │
│  │  WHERE datistemplate = false                                          │ │
│  │  AND datallowconn = true;                                             │ │
│  │                                                                       │ │
│  │  [CodeMirror 6 — tema escuro, syntax highlighting SQL]                │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ ⚡ Query Type: [SELECT]  |  Destructive: [NO]  |  Tables: 1           │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│                                    ┌──────────────┐                        │
│                                    │ ▶  Next Step │                        │
│                                    └──────────────┘                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **CodeMirror 6** com `@codemirror/lang-sql` para syntax highlighting
- **Query analyzer em tempo real** via `POST /api/query/analyze`
- Botão "Next" desabilitado se SQL vazio ou inválido

### Passo 2 — Seleção de Servidores

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🐺 FurLab                                          [—] [□] [×]             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ◄── Back to Editor          Step 2 of 4 ──►                              │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  🖥️  Servers                                                           │ │
│  │                                                                       │ │
│  │  ☑️  [prod-db-01]  postgres@prod-db-01.company.com:5432                │ │
│  │      Databases: [app_1] [app_2] [analytics]                           │ │
│  │                                                                       │ │
│  │  ☑️  [prod-db-02]  postgres@prod-db-02.company.com:5432                │ │
│  │      Databases: [app_1] [app_2]                                       │ │
│  │                                                                       │ │
│  │  ☐  [staging-01]   postgres@staging.company.com:5432                   │ │
│  │      Databases: [app_staging]                                         │ │
│  │                                                                       │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  [🔄 Fetch all databases]  [⚙️ Advanced options]                       │ │
│  │                                                                       │ │
│  │  Estimated databases: ~5 (2 servers × manual selection)               │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│                                    ┌──────────────┐                        │
│                                    │ ▶  Run Query │                        │
│                                    └──────────────┘                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Lista de servidores de `GET /api/servers`
- Multi-select com checkboxes (todos pré-selecionados)
- "Fetch all databases" replica o comportamento `--all` da CLI
- "Advanced options" colapsado: timeout, connection overrides

### Passo 3 — Execução em Tempo Real

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🐺 FurLab                                          [—] [□] [×]             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ◄── Cancel execution                                                     │
│                                                                             │
│  Overall Progress: [████████████████████░░░░░░░░░░]  67% (8/12 DBs)       │
│                                                                             │
│  ┌────────┬──────────────┬────────────────┬──────┬──────────┐             │
│  │ Status │ Server       │ Database       │ Rows │ Duration │             │
│  ├────────┼──────────────┼────────────────┼──────┼──────────┤             │
│  │   ✅   │ prod-db-01   │ app_1          │ 450  │ 1.2s     │             │
│  │   ✅   │ prod-db-01   │ app_2          │ 12   │ 0.8s     │             │
│  │   ✅   │ prod-db-01   │ analytics      │ 0    │ 0.5s     │             │
│  │   ✅   │ prod-db-02   │ app_1          │ 450  │ 1.5s     │             │
│  │   ✅   │ prod-db-02   │ app_2          │ 12   │ 0.9s     │             │
│  │   ⏳   │ prod-db-02   │ analytics      │ ...  │ ...      │  ← running  │
│  │   ⏳   │ prod-db-02   │ reporting      │ ...  │ ...      │  ← running  │
│  │   ❌   │ prod-db-02   │ warehouse      │ ERR  │ 3.1s     │  ← timeout! │
│  │   ⏳   │ prod-db-02   │ archive        │ ...  │ ...      │  ← retry 1/3│
│  └────────┴──────────────┴────────────────┴──────┴──────────┘             │
│                                                                             │
│  Servers: 2  |  Success: 5  |  Failed: 1  |  Running: 3                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Progress bar global derivada dos eventos `progress` e `result`
- Tabela ao vivo — cada evento SSE atualiza uma linha
- Ícones de status: ✅ sucesso, ❌ erro, ⏳ rodando, 🔄 retry

### Passo 4 — Resultados Consolidados

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🐺 FurLab                                          [—] [□] [×]             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ◄── New Query               Query Results                                │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  ✅ Completed in 12.4s                                                │ │
│  │  2 servers | 11 success | 1 failed | 924 total rows                   │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  🔍 Filter: [________________________]  [Columns ▼] [Export ▼]        │ │
│  │                                                                       │ │
│  │  ┌────────┬────────┬────────────────┬────────┬────────┐              │ │
│  │  │ Server │ DB     │ datname        │ size   │ ...    │              │ │
│  │  ├────────┼────────┼────────────────┼────────┼────────┤              │ │
│  │  │ prod-1 │ app_1  │ app_1          │ 45MB   │ ...    │              │ │
│  │  │ prod-1 │ app_2  │ app_2          │ 12MB   │ ...    │              │ │
│  │  │ prod-2 │ app_1  │ app_1          │ 45MB   │ ...    │              │ │
│  │  │ ...    │ ...    │ ...            │ ...    │ ...    │              │ │
│  │  └────────┴────────┴────────────────┴────────┴────────┘              │ │
│  │                                                                       │ │
│  │  [Material Paginator: 1-50 of 924 rows]                               │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  💾 Output: C:\Users\...\2025-04-30_143022\                           │ │
│  │     [📁 Open Folder]  [📄 consolidated_20250430_143022.csv]           │ │
│  │     [❌ errors_20250430_143022.csv]  [📝 log_20250430_143022]         │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│                                    ┌──────────────┐                        │
│                                    │ 🔄 Run Again │                        │
│                                    └──────────────┘                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Tabela consolidada: primeira coluna "Server", segunda "Database", depois colunas do resultado
- Filtro + paginação com Angular Material Table
- "Open Folder" via Tauri IPC; download CSV via API ou IPC

---

## 7. State Management Angular (Nativo)

Sem NgRx. Apenas RxJS + Angular DI.

### 7.1. Diagrama de Serviços

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         SERVIÇOS E ESTADO                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  QueryStateService (providedIn: 'root') — SINGLE SOURCE OF TRUTH      │ │
│  │                                                                       │ │
│  │  private _state = new BehaviorSubject<QueryState>({                   │ │
│  │    step: 'idle', sql: '', selectedServers: [],                        │ │
│  │    fetchAllDatabases: false, events: [], results: [],                 │ │
│  │    isRunning: false, error: null, lastEventTime: 0                    │ │
│  │  });                                                                  │ │
│  │                                                                       │ │
│  │  readonly state$ = _state.asObservable();                             │ │
│  │                                                                       │ │
│  │  // Selectors (derivados com map + distinctUntilChanged)              │ │
│  │  readonly currentStep$ = state$.pipe(map(s => s.step));               │ │
│  │  readonly progress$ = state$.pipe(                                    │ │
│  │    map(s => {                                                         │ │
│  │      const completed = s.events.filter(e => e.type === 'result').length│ │
│  │      const start = s.events.find(e => e.type === 'start');            │ │
│  │      return { current: completed, total: start?.totalDatabases ?? 0 } │ │
│  │    }),                                                                │ │
│  │    distinctUntilChanged()                                             │ │
│  │  );                                                                   │ │
│  │                                                                       │ │
│  │  // Actions                                                           │ │
│  │  setSql(sql) { patchState({ sql }); }                                 │ │
│  │  setServers(servers) { patchState({ selectedServers: servers }); }    │ │
│  │  addEvent(event) {                                                    │ │
│  │    patchState({                                                       │ │
│  │      events: [..._state.value.events, event],                         │ │
│  │      lastEventTime: Date.now()                                        │ │
│  │    });                                                                │ │
│  │  }                                                                    │ │
│  │  reset() { _state.next(initialState); }                               │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  QueryOrchestratorService — COORDENA O FLUXO SSE                      │ │
│  │                                                                       │ │
│  │  async startExecution() {                                             │ │
│  │    const state = queryState.snapshot;                                 │ │
│  │    queryState.patchState({ isRunning: true, step: 'running' });       │ │
│  │                                                                       │ │
│  │    const stream = await queryService.execute({...});                  │ │
│  │    const reader = stream.getReader();                                 │ │
│  │                                                                       │ │
│  │    while (true) {                                                     │ │
│  │      const { done, value: event } = await reader.read();              │ │
│  │      if (done) break;                                                 │ │
│  │      queryState.addEvent(event);                                      │ │
│  │      if (event.type === 'complete') {                                 │ │
│  │        queryState.patchState({ step: 'results' });                    │ │
│  │      }                                                                │ │
│  │    }                                                                  │ │
│  │  }                                                                    │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  QueryService — CONSUMO NDJSON                                        │ │
│  │                                                                       │ │
│  │  async execute(options): Promise<ReadableStream<QueryEvent>> {        │ │
│  │    const response = await fetch(`/api/query/execute?${params}`, {     │ │
│  │      headers: { Accept: 'application/x-ndjson' }                      │ │
│  │    });                                                                │ │
│  │    const reader = response.body.getReader();                          │ │
│  │    const decoder = new TextDecoder();                                 │ │
│  │                                                                       │ │
│  │    return new ReadableStream({                                        │ │
│  │      async pull(controller) {                                         │ │
│  │        const { done, value } = await reader.read();                   │ │
│  │        if (done) { controller.close(); return; }                      │ │
│  │        const lines = decoder.decode(value, {stream: true})            │ │
│  │                      .split('\n').filter(l => l.trim());             │ │
│  │        for (const line of lines) {                                    │ │
│  │          controller.enqueue(JSON.parse(line));                        │ │
│  │        }                                                              │ │
│  │      }                                                                │ │
│  │    });                                                                │ │
│  │  }                                                                    │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  ServerService — REST SIMPLES                                         │ │
│  │                                                                       │ │
│  │  getServers(): Observable<Server[]> {                                 │ │
│  │    return this.http.get<Server[]>('/api/servers');                    │ │
│  │  }                                                                    │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.2. State Machine do Componente

```
                    ┌─────────────┐
                    │    IDLE     │
                    │(editor vazio│
                    │ ou com SQL) │
                    └──────┬──────┘
                           │ user types SQL
                           ▼
                    ┌─────────────┐
                    │  VALID_SQL  │
                    │(analyzer ok)│
                    └──────┬──────┘
                           │ clicks "Next"
                           ▼
                    ┌─────────────┐
                    │  SELECTING  │
                    │(servers e  │
                    │  options)   │
                    └──────┬──────┘
                           │ clicks "Run"
                           ▼
                    ┌─────────────┐
                    │  RUNNING    │◄──────────┐
                    │(SSE stream │           │ retry event
                    │  active)    │           │
                    └──────┬──────┘           │
                           │                  │
          ┌────────────────┼────────────────┐ │
          │                │                │ │
          ▼                ▼                ▼ │
     ┌────────┐      ┌────────┐      ┌────────┐
     │SUCCESS │      │ FAILED │      │CANCELLED│
     └────────┘      └────────┘      └────────┘
          │                │                │
          └────────────────┴────────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │   RESULTS   │
                    │(show table) │
                    └─────────────┘
```

---

## 8. Tauri IPC — Comandos Nativos

### 8.1. Handlers Rust

```rust
// src-tauri/src/main.rs

use tauri::{Manager, command};
use std::process::Command as ProcessCommand;
use regex::Regex;

// ── Descoberta de porta ─────────────────────────────────────────────

#[derive(Default)]
struct ApiUrl(String);

#[derive(Default)]
struct ApiSecret(String);

#[command]
fn get_api_url(state: tauri::State<ApiUrl>) -> String {
    state.0.clone()
}

#[command]
fn get_api_secret(state: tauri::State<ApiSecret>) -> String {
    state.0.clone()
}

// ── File System ────────────────────────────────────────────────────

#[command]
async fn show_in_folder(path: String) -> Result<(), String> {
    #[cfg(target_os = "windows")]
    {
        ProcessCommand::new("explorer")
            .args(["/select,", &path])
            .spawn()
            .map_err(|e| e.to_string())?;
    }

    #[cfg(target_os = "macos")]
    {
        ProcessCommand::new("open")
            .args(["-R", &path])
            .spawn()
            .map_err(|e| e.to_string())?;
    }

    #[cfg(target_os = "linux")]
    {
        let parent = std::path::Path::new(&path)
            .parent()
            .unwrap_or(std::path::Path::new("/"));
        ProcessCommand::new("xdg-open")
            .arg(parent)
            .spawn()
            .map_err(|e| e.to_string())?;
    }

    Ok(())
}

#[command]
async fn save_file_dialog(
    default_name: String,
) -> Result<Option<String>, String> {
    use tauri::api::dialog::FileDialogBuilder;

    let path = FileDialogBuilder::new()
        .set_file_name(&default_name)
        .add_filter("CSV", &["csv"])
        .save_file();

    Ok(path.map(|p| p.to_string_lossy().to_string()))
}

// ── Main ───────────────────────────────────────────────────────────

fn main() {
    tauri::Builder::default()
        .setup(|app| {
            let sidecar = app.shell().sidecar("FurLab.Api").unwrap();
            let (mut rx, _child) = sidecar
                .args(["--urls", "http://127.0.0.1:0"])
                .spawn()
                .expect("Failed to start FurLab.Api sidecar");

            let re = Regex::new(r"\[FurLab\.Api\] Listening on (http://[\d\.]+:\d+)").unwrap();

            tauri::async_runtime::spawn(async move {
                while let Some(event) = rx.recv().await {
                    if let tauri::api::process::CommandEvent::Stdout(line) = event {
                        if let Some(caps) = re.captures(&line) {
                            let api_url = caps.get(1).unwrap().as_str().to_string();
                            app.manage(ApiUrl(api_url));
                            app.emit_all("api-ready", api_url).unwrap();
                            break;
                        }
                    }
                }
            });

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            get_api_url,
            get_api_secret,
            show_in_folder,
            save_file_dialog
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
```

### 8.2. Serviço Angular para IPC

```typescript
// src-ui/src/app/core/services/tauri-shell.service.ts

import { Injectable } from '@angular/core';
import { invoke } from '@tauri-apps/api/tauri';

@Injectable({ providedIn: 'root' })
export class TauriShellService {
  async showInFolder(path: string): Promise<void> {
    if (!this.isTauri()) {
      console.warn('showInFolder only works in desktop mode');
      return;
    }
    await invoke('show_in_folder', { path });
  }

  async saveFileDialog(defaultName: string): Promise<string | null> {
    if (!this.isTauri()) return null;
    return await invoke('save_file_dialog', { defaultName });
  }

  private isTauri(): boolean {
    return !!(window as any).__TAURI__;
  }
}
```

---

## 9. Download de CSV: Estratégia Híbrida

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         DOWNLOAD CSV: DESKTOP vs WEB                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  NO DESKTOP (Tauri):                                                        │
│  ───────────────────                                                        │
│                                                                             │
│  Angular ──invoke──► Tauri Rust ──std::fs──► Copia arquivo para            │
│  "Salvar Como"        dialog nativo            pasta escolhida pelo user   │
│                                                                             │
│  • Zero memória no browser                                                  │
│  • Streaming direto disco-a-disco                                           │
│  • UX nativa (Save As dialog do Windows/macOS/Linux)                        │
│                                                                             │
│                                                                             │
│  NA WEB (fur gui --web):                                                    │
│  ────────────────────────                                                   │
│                                                                             │
│  Angular ──fetch──► FurLab.Api ──FileResult──► Blob no browser             │
│  "Download"           /api/query/download      → saveAs()                   │
│                                                                             │
│  • Compatível com qualquer navegador                                        │
│  • Arquivo passa por memória como blob                                      │
│  • OK para arquivos até ~100MB                                              │
│                                                                             │
│                                                                             │
│  Abstração no Angular:                                                      │
│  ─────────────────────                                                      │
│                                                                             │
│  export class FileDownloadService {                                         │
│    async downloadCsv(sourcePath: string, filename: string) {                │
│      if (isTauri()) {                                                       │
│        const dest = await tauriShell.saveFileDialog(filename);              │
│        if (dest) await tauriShell.copyFile(sourcePath, dest);               │
│      } else {                                                               │
│        await this.downloadViaApi(filename);                                 │
│      }                                                                      │
│    }                                                                        │
│  }                                                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Error Handling no SSE

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         ERROR HANDLING: O QUE PODE DAR ERRADO E COMO LIDAR                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Cenário 1: API crasha no meio do streaming                                │
│  ─────────────────────────────────────────                                 │
│  SINTOMA: Conexão fecha sem evento "complete"                              │
│  SOLUÇÃO: Servidor envia heartbeat "ping" a cada 5s                        │
│           Cliente watchdog: se nada recebido em 10s → connection lost      │
│                                                                             │
│  Cenário 2: Laptop fecha (sleep/resume)                                    │
│  ───────────────────────────────────────                                   │
│  SINTOMA: Conexão TCP fica "zumbi" — nem aberta nem fechada                │
│  SOLUÇÃO: Mesmo watchdog de 10s; se atingido, considera erro               │
│                                                                             │
│  Cenário 3: Usuário clica Cancelar                                         │
│  ─────────────────────────────────                                         │
│  SOLUÇÃO: Double-cancel                                                    │
│    1. AbortController.abort() no fetch (fecha conexão no cliente)          │
│    2. POST /api/query/cancel/{sessionId} (sinaliza no servidor)            │
│    3. CancellationToken no .NET propaga para Parallel.ForEachAsync         │
│                                                                             │
│  Implementação do watchdog no Angular:                                     │
│  ─────────────────────────────────────                                     │
│                                                                             │
│  const watchdog = setInterval(() => {                                      │
│    const last = this.queryState.snapshot.lastEventTime;                    │
│    if (Date.now() - last > 10000) {                                        │
│      this.queryOrchestrator.handleConnectionLost();                        │
│      this.queryState.patchState({                                          │
│        step: 'error',                                                      │
│        error: 'Connection lost. The server may have stopped.',             │
│        isRunning: false                                                    │
│      });                                                                   │
│    }                                                                       │
│  }, 5000);                                                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 11. CLI: Mantida Direta (Não Consome API)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         CLI: PERMANECE INALTERADA NO ACESSO AO CORE                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Decisão arquitetural: a FurLab.CLI NÃO consome a FurLab.Api.              │
│                                                                             │
│  Motivos:                                                                   │
│  1. A CLI é o produto principal e funciona perfeitamente                   │
│  2. Adicionar HTTP à CLI aumentaria complexidade sem ganho real            │
│  3. Startup da CLI seria impactado (~500ms-1s para subir Kestrel)          │
│  4. Não há necessidade de unificação no MVP                                │
│                                                                             │
│  Futuro (fase distante):                                                    │
│  • Se surgir necessidade de "modo servidor" (CLI inicia API em background) │
│  • Ou se criar uma VS Code Extension que consuma a API                     │
│  • Aí a unificação faz sentido                                             │
│                                                                             │
│  Por enquanto:                                                              │
│                                                                             │
│     FurLab.CLI ──► FurLab.Core  (direto)                                   │
│     FurLab.Api ──► FurLab.Core  (direto)                                   │
│     Angular    ──► FurLab.Api   (HTTP)                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Fluxo de Desenvolvimento

### 12.1. Fase 1 — Desenvolvimento Ativo (primeiras semanas)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         FASE 1: DEV COM 3 TERMINAIS (HOT RELOAD)                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Terminal 1: API .NET                                                       │
│  $ dotnet run --project FurLab.Api/FurLab.Api.csproj --urls "http://127.0.0.1:5001"
│                                                                             │
│  Terminal 2: Angular dev server                                             │
│  $ cd src-ui && npm start                                                  │
│  → http://localhost:4200                                                   │
│                                                                             │
│  Terminal 3: Tauri (aponta para ng serve)                                  │
│  $ cd src-tauri && cargo tauri dev                                         │
│  → WebView aponta para http://localhost:4200                               │
│  → API aponta para http://localhost:5001 (hardcoded no dev)                │
│                                                                             │
│  Vantagens:                                                                 │
│  • Hot reload do Angular funciona (<1s)                                    │
│  • API debugável isoladamente no VS Code                                   │
│  • Tauri recompila automaticamente ao editar Rust                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 12.2. Fase 2 — Pré-Produção (sidecar auto-spawn)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         FASE 2: TAURI DEV COM SIDECAR AUTO-SPAWN                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Único comando:                                                             │
│  $ cd src-tauri && cargo tauri dev                                         │
│                                                                             │
│  O Tauri:                                                                   │
│  1. Roda beforeDevCommand: "cd ../src-ui && npm start"                    │
│  2. Spawna sidecar FurLab.Api automaticamente                              │
│  3. Parseia stdout para descobrir porta                                    │
│  4. Passa URL para Angular via evento "api-ready"                          │
│                                                                             │
│  Usado para testar o fluxo completo de empacotamento antes do release.     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 12.3. Build de Release

```powershell
# scripts/build-desktop.ps1

param([switch]$Release)

# 1. Build da API
$config = if ($Release) { "Release" } else { "Debug" }
dotnet build FurLab.Api/FurLab.Api.csproj --configuration $config --output src-tauri/binaries

# Renomeia para formato de sidecar do Tauri
$src = "src-tauri/binaries/FurLab.Api.exe"
$dst = "src-tauri/binaries/FurLab-x86_64-pc-windows-msvc.exe"
Move-Item $src $dst -Force

# 2. Build do Angular
Set-Location src-ui
npm install
npm run build -- --configuration $(if ($Release) { "production" } else { "development" })
Set-Location ..

# 3. Build do Tauri
Set-Location src-tauri
if ($Release) {
    cargo tauri build
} else {
    cargo tauri build --debug
}
Set-Location ..
```

---

## 13. MVP Scope — O Que Entra e O Que Fica Para Depois

```
┌─────────────────────────────────────────────────────────────────────────────┐
│         MVP SCOPE: QUERY RUN DESKTOP                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✅ INCLUÍDO NO MVP                                                         │
│  ─────────────────                                                          │
│                                                                             │
│  Backend (FurLab.Api):                                                      │
│  • GET /api/servers                                                         │
│  • POST /api/query/analyze                                                  │
│  • GET /api/query/execute (NDJSON streaming)                                │
│  • POST /api/query/cancel/{sessionId}                                       │
│  • GET /api/query/download/{sessionId}                                      │
│  • Localhost-only middleware                                                │
│  • Sidecar auto-spawn no Tauri                                              │
│                                                                             │
│  Frontend (Angular 21):                                                     │
│  • Tela Query Run com 4 passos (Editor → Servers → Execute → Results)      │
│  • CodeMirror 6 para SQL                                                    │
│  • Seleção de servidores (do furlab.jsonc)                                  │
│  • Streaming de progresso em tempo real (NDJSON)                            │
│  • Tabela de resultados consolidados com Material Table                     │
│  • Download CSV via API                                                     │
│                                                                             │
│  Desktop (Tauri 2):                                                         │
│  • Janela nativa 1400x900                                                   │
│  • Sidecar .NET auto-spawn                                                  │
│  • Descoberta de porta via stdout                                           │
│  • "Open folder" via IPC                                                    │
│                                                                             │
│  ❌ FORA DO MVP (roadmap futuro)                                            │
│  ────────────────────────────────                                           │
│                                                                             │
│  • Database Backup/Restore GUI                                              │
│  • File Combine/Convert GUI                                                 │
│  • Winget Backup/Restore GUI                                                │
│  • Settings management na GUI (CRUD de servidores)                          │
│  • Query history / saved queries                                            │
│  • Monaco Editor (mantém CodeMirror)                                        │
│  • Dark/light mode toggle                                                   │
│  • Auto-updater do Tauri                                                    │
│  • Modo web (`fur gui --web`)                                               │
│  • CLI consumindo a API                                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 14. Checklist de Decisões Arquiteturais

| # | Decisão | Alternativa Rejeitada | Por quê |
|---|---------|----------------------|---------|
| 1 | **Tauri 2** em vez de Electron | Electron 41 + electron-builder | Bundle 5MB vs 150MB; memória mínima; WebView nativa |
| 2 | **Angular 21** | Vue, Svelte, Solid | Build to learn; Material Design maduro; familiaridade |
| 3 | **NDJSON** em vez de SSE/EventSource | SSE `data:` prefix, SignalR, gRPC | Menos bundle; menos código servidor; funciona em proxies |
| 4 | **CodeMirror 6** em vez de Monaco | Monaco Editor | Bundle 200KB vs 3-5MB; SQL não precisa de IntelliSense |
| 5 | **Tauri sidecar** em vez de .NET principal | .NET hospeda Tauri WebView | Tauri orquestra melhor (tray, updater, lifecycle) |
| 6 | **Porta aleatória** + stdout parsing | Porta fixa | Evita conflitos; idiomaticamente correto |
| 7 | **BehaviorSubject** em vez de NgRx | NgRx, Akita | Dentro do framework Angular; sem dependências extras |
| 8 | **CLI direta** em vez de CLI→API | CLI consome HTTP | Não quebrar o que funciona; menor risco no MVP |
| 9 | **Download híbrido** (API + IPC) | Só API ou só IPC | Melhor UX no desktop; compatibilidade no web |
| 10 | **Localhost only** em vez de auth | JWT, token-based | App local; defesa em profundidade com bind + middleware |
| 11 | **Query Run como MVP** | Todas as features de uma vez | Escopo controlado; query run é o fluxo mais valioso |

---

## 15. Referências e Links Úteis

- [Tauri 2 Docs](https://v2.tauri.app/)
- [Angular 21 Docs](https://angular.dev/)
- [Angular Material](https://material.angular.io/)
- [CodeMirror 6](https://codemirror.net/)
- [ASP.NET Core Streaming](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)
- [NDJSON Spec](https://github.com/ndjson/ndjson-spec)
- Plano anterior: `docs/pt-BR/GUI_REFACTORING_PLAN.md`
- Resumo de sessão (Core extraído): `docs/pt-BR/SESSION_SUMMARY_2025-03-17.md`

---

*Documento criado em sessão de exploração. Próximo passo sugerido: criar OpenSpec change `add-desktop-gui` com proposal, design, specs e tasks para início da implementação.*
