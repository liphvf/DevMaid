## 1. Projeto e Infraestrutura da API

- [x] 1.1 Criar projeto `FurLab.Api` (ASP.NET Core Empty, `net10.0`)
- [x] 1.2 Adicionar referência de projeto para `FurLab.Core`
- [x] 1.3 Adicionar `FurLab.Api` à solução `FurLab.slnx`
- [x] 1.4 Configurar `Program.cs` com Kestrel bind localhost (`ListenLocalhost`)
- [x] 1.5 Criar `LocalhostSecurityMiddleware` que verifica `RemoteIpAddress` e retorna 403 para IPs não-loopback
- [x] 1.6 Registrar serviços da Core no DI da API (`AddFurLabServices`)

## 2. Extração do Motor de Execução para Core

- [x] 2.1 Criar interface `IProgressObserverService` em `FurLab.Core/Interfaces`
- [x] 2.2 Criar `QueryPlannerService` em `FurLab.Core/Services` (descoberta de databases, filtro de excludes)
- [x] 2.3 Criar `QueryExecutorService` em `FurLab.Core/Services` (paralelismo, Polly retry, cancelamento)
- [x] 2.4 Renomear/refatorar `CsvExporter` para `CsvExporterService` e movê-lo para `FurLab.Core/Services`
- [x] 2.5 Criar `ExecutionContext` record em `FurLab.Core/Models` (estado de uma execução)
- [x] 2.6 Criar DTOs da API em `FurLab.Core/Models` (`QueryAnalyzeRequest`, `QueryExecuteRequest`, `QueryStatusResponse`, etc.)
- [x] 2.7 Refatorar `QueryRunCommand` do CLI para usar `QueryPlannerService` e `QueryExecutorService`
- [x] 2.8 Criar `ConsoleObserverService` em `FurLab.CLI` que implementa `IProgressObserverService` via Spectre.Console
- [x] 2.9 Validar que CLI continua funcionando (build + testes existentes)

## 3. Serviços da API

- [x] 3.1 Criar `ExecutionRegistryService` (`ConcurrentDictionary<Guid, ExecutionContext>`)
- [x] 3.2 Criar `SseBroadcasterService` (gerencia canais SSE por `executionId`)
- [x] 3.3 Criar `SseEventSinkService` que implementa `IProgressObserverService` e emite eventos SSE
- [x] 3.4 Criar `StatusTrackerService` que mantém estado atualizado para `GET /status`
- [x] 3.5 Implementar lógica de cancelamento via `CancellationTokenSource` no `ExecutionContext`

## 4. Controllers e Endpoints

- [x] 4.1 Criar `QueryController` com `POST /api/query/analyze`
- [x] 4.2 Criar `POST /api/query/execute` (valida request, inicia execução, retorna `executionId`)
- [x] 4.3 Criar `GET /api/query/status` (consulta `ExecutionRegistryService`, retorna estado atual)
- [x] 4.4 Criar `GET /api/query/events` endpoint SSE (streaming via `SseBroadcasterService`)
- [x] 4.5 Criar `POST /api/query/cancel` (sinaliza `CancellationTokenSource`, retorna status)
- [x] 4.6 Criar `GET /api/query/download` (retorna arquivo CSV do disco via `FileStreamResult`)

## 5. Configuração e Segurança

- [x] 5.1 Configurar CORS mínimo (ou ausente) já que é localhost-only
- [x] 5.2 Adicionar `appsettings.json` e `appsettings.Development.json` no `FurLab.Api`
- [x] 5.3 Garantir que a API não expõe stack traces em produção (modo Release) — ASP.NET Core não expõe por padrão sem DeveloperExceptionPage
- [x] 5.4 Adicionar validação de modelo (`[ApiController]` + `[ModelState]`)

## 6. Testes

- [x] 6.1 Criar testes unitários para serviços da API (ExecutionRegistry, StatusTracker, SSE)
- [x] 6.2 Criar testes unitários para `LocalhostSecurityMiddleware` (aceita loopback, rejeita externo)
- [x] 6.3 Criar testes unitários para `QueryController` (analyze, execute, status, cancel, download)
- [x] 6.4 Criar testes para `SseEventSinkService` (eventos broadcast e status tracking)
- [x] 6.5 Garantir que todos os testes existentes do CLI continuam passando
- [x] 6.6 Todos os testes passam (176/176)
- [x] 6.7 Criar testes de integração com Testcontainers (PostgreSQL efêmero, porta aleatória)
- [x] 6.8 Marcar testes de integração com `[TestCategory("Integration")]`
- [x] 6.9 Filtrar testes de integração no GitHub Actions (`--filter "TestCategory!=Integration"`)
- [x] 6.10 Criar workflow separado para testes de integração semanal/manual

## 7. Documentação e Finalização

- [x] 7.1 Documentar endpoints no `README.md` ou arquivo dedicado em `docs/`
- [x] 7.2 Verificar build completo da solução (`dotnet build`)
- [x] 7.3 Executar `dotnet format` para garantir estilo de código
- [x] 7.4 Revisar se todas as convenções do `CLAUDE.md` foram seguidas (nomenclatura `*Service`, etc.)
