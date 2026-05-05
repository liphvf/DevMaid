## 1. Projeto e Infraestrutura da API

- [ ] 1.1 Criar projeto `FurLab.Api` (ASP.NET Core Empty, `net10.0`)
- [ ] 1.2 Adicionar referência de projeto para `FurLab.Core`
- [ ] 1.3 Adicionar `FurLab.Api` à solução `FurLab.slnx`
- [ ] 1.4 Configurar `Program.cs` com Kestrel bind localhost (`ListenLocalhost`)
- [ ] 1.5 Criar `LocalhostSecurityMiddleware` que verifica `RemoteIpAddress` e retorna 403 para IPs não-loopback
- [ ] 1.6 Registrar serviços da Core no DI da API (`AddFurLabServices`)

## 2. Extração do Motor de Execução para Core

- [ ] 2.1 Criar interface `IProgressObserverService` em `FurLab.Core/Interfaces`
- [ ] 2.2 Criar `QueryPlannerService` em `FurLab.Core/Services` (descoberta de databases, filtro de excludes)
- [ ] 2.3 Criar `QueryExecutorService` em `FurLab.Core/Services` (paralelismo, Polly retry, cancelamento)
- [ ] 2.4 Renomear/refatorar `CsvExporter` para `CsvExporterService` e movê-lo para `FurLab.Core/Services`
- [ ] 2.5 Criar `ExecutionContext` record em `FurLab.Core/Models` (estado de uma execução)
- [ ] 2.6 Criar DTOs da API em `FurLab.Core/Models` (`QueryAnalyzeRequest`, `QueryExecuteRequest`, `QueryStatusResponse`, etc.)
- [ ] 2.7 Refatorar `QueryRunCommand` do CLI para usar `QueryPlannerService` e `QueryExecutorService`
- [ ] 2.8 Criar `ConsoleObserverService` em `FurLab.CLI` que implementa `IProgressObserverService` via Spectre.Console
- [ ] 2.9 Validar que CLI continua funcionando (build + testes existentes)

## 3. Serviços da API

- [ ] 3.1 Criar `ExecutionRegistryService` (`ConcurrentDictionary<Guid, ExecutionContext>`)
- [ ] 3.2 Criar `SseBroadcasterService` (gerencia canais SSE por `executionId`)
- [ ] 3.3 Criar `SseEventSinkService` que implementa `IProgressObserverService` e emite eventos SSE
- [ ] 3.4 Criar `StatusTrackerService` que mantém estado atualizado para `GET /status`
- [ ] 3.5 Implementar lógica de cancelamento via `CancellationTokenSource` no `ExecutionContext`

## 4. Controllers e Endpoints

- [ ] 4.1 Criar `QueryController` com `POST /api/query/analyze`
- [ ] 4.2 Criar `POST /api/query/execute` (valida request, inicia execução, retorna `executionId`)
- [ ] 4.3 Criar `GET /api/query/status` (consulta `ExecutionRegistryService`, retorna estado atual)
- [ ] 4.4 Criar `GET /api/query/events` endpoint SSE (streaming via `SseBroadcasterService`)
- [ ] 4.5 Criar `POST /api/query/cancel` (sinaliza `CancellationTokenSource`, retorna status)
- [ ] 4.6 Criar `GET /api/query/download` (retorna arquivo CSV do disco via `FileStreamResult`)

## 5. Configuração e Segurança

- [ ] 5.1 Configurar CORS mínimo (ou ausente) já que é localhost-only
- [ ] 5.2 Adicionar `appsettings.json` e `appsettings.Development.json` no `FurLab.Api`
- [ ] 5.3 Garantir que a API não expõe stack traces em produção (modo Release)
- [ ] 5.4 Adicionar validação de modelo (`[ApiController]` + `[ModelState]`)

## 6. Testes

- [ ] 6.1 Criar testes unitários para `QueryPlannerService` (descoberta de DBs, filtros)
- [ ] 6.2 Criar testes unitários para `QueryExecutorService` (paralelismo, retry, cancelamento)
- [ ] 6.3 Criar testes de integração para `QueryController` (análise, execução, status, download)
- [ ] 6.4 Criar teste para `LocalhostSecurityMiddleware` (aceita loopback, rejeita externo)
- [ ] 6.5 Garantir que todos os testes existentes do CLI continuam passando
- [ ] 6.6 Validar SSE via teste de integração (verificar eventos emitidos em ordem)

## 7. Documentação e Finalização

- [ ] 7.1 Documentar endpoints no `README.md` ou arquivo dedicado em `docs/`
- [ ] 7.2 Verificar build completo da solução (`dotnet build`)
- [ ] 7.3 Executar `dotnet format` para garantir estilo de código
- [ ] 7.4 Revisar se todas as convenções do `CLAUDE.md` foram seguidas (nomenclatura `*Service`, etc.)
