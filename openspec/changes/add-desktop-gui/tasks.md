## 1. Setup de Projetos

- [ ] 1.1 Criar projeto `FurLab.Api` com `dotnet new webapi -n FurLab.Api -f net10.0`
- [ ] 1.2 Adicionar `FurLab.Api` ao `FurLab.slnx`
- [ ] 1.3 Adicionar `<ProjectReference>` para `FurLab.Core` no `FurLab.Api.csproj`
- [ ] 1.4 Criar pasta `src-ui/` e inicializar projeto Angular 21 com `ng new`
- [ ] 1.5 Instalar Angular Material 21 no `src-ui/`
- [ ] 1.6 Instalar CodeMirror 6 (`codemirror`, `@codemirror/lang-sql`, `@codemirror/theme-one-dark`) no `src-ui/`
- [ ] 1.7 Criar pasta `src-tauri/` e inicializar projeto Tauri 2 com `cargo tauri init`
- [ ] 1.8 Configurar `tauri.conf.json` com `frontendDist`, `devUrl`, `bundle.externalBin` e CSP

## 2. Backend API — Infraestrutura

- [ ] 2.1 Configurar Kestrel para `http://127.0.0.1:0` e logar porta no stdout (`[FurLab.Api] Listening on ...`)
- [ ] 2.2 Implementar `LocalhostOnlyMiddleware` para rejeitar requisições não-loopback (HTTP 403)
- [ ] 2.3 Configurar CORS para permitir origens `http://localhost:*` e `tauri://localhost`
- [ ] 2.4 Criar `appsettings.json` do `FurLab.Api` com configurações de logging e Kestrel
- [ ] 2.5 Registrar serviços do `FurLab.Core` no DI (`IUserConfigService`, `ICredentialService`, etc.)

## 3. Backend API — Controllers

- [ ] 3.1 Implementar `GET /api/servers` em `ServersController` (delega para `IUserConfigService.GetServers()`)
- [ ] 3.2 Implementar `POST /api/query/analyze` em `QueryController` (usa `SqlQueryAnalyzer` do Core)
- [ ] 3.3 Implementar `POST /api/query/preview` em `QueryController` (discovery de databases sem executar query)
- [ ] 3.4 Criar models C# `QueryEvent` e derivados (`StartEvent`, `ProgressEvent`, `ResultEvent`, etc.) no `FurLab.Api`
- [ ] 3.5 Extrair lógica de execução de query de `QueryRunCommand` (CLI) para novo `IQueryExecutionService` no `FurLab.Core`
- [ ] 3.6 Implementar `GET /api/query/execute` com `IAsyncEnumerable<QueryEvent>` e NDJSON streaming (consome `IQueryExecutionService`)
- [ ] 3.7 Implementar `POST /api/query/cancel/{sessionId}` com `CancellationTokenSource`
- [ ] 3.8 Implementar `GET /api/query/download/{sessionId}?type=consolidated|errors|log` com `FileResult`

## 4. Frontend Angular — Core Services

- [ ] 4.1 Criar `ApiUrlService` para descoberta de porta via evento Tauri `api-ready`
- [ ] 4.2 Criar `ApiInterceptor` para injetar URL base da API em todas as requisições HttpClient
- [ ] 4.3 Criar `ServerService` com `getServers(): Observable<Server[]>`
- [ ] 4.4 Criar `QueryService` com `execute(options): Promise<ReadableStream<QueryEvent>>` (consumo NDJSON)
- [ ] 4.5 Criar `QueryStateService` com `BehaviorSubject<QueryState>` e selectors derivados
- [ ] 4.6 Criar `QueryOrchestratorService` para coordenar o fluxo SSE + atualização de estado
- [ ] 4.7 Criar `TauriShellService` com `showInFolder(path)` e `saveFileDialog(defaultName)` via `invoke`
- [ ] 4.8 Criar interfaces TypeScript para todos os `QueryEvent` (`StartEvent`, `ProgressEvent`, `ResultEvent`, etc.)

## 5. Frontend Angular — Componentes da Query Run

- [ ] 5.1 Criar `QueryEditorComponent` com CodeMirror 6 wrapper e evento `change`
- [ ] 5.2 Criar `ServerSelectorComponent` com lista de checkboxes e toggle "Fetch all databases"
- [ ] 5.3 Criar `QueryProgressComponent` com barra de progresso geral e tabela ao vivo (status ✅❌⏳🔄)
- [ ] 5.4 Criar `QueryResultsComponent` com Material Table, filtro, paginação e resumo numérico
- [ ] 5.5 Criar `QueryRunComponent` (orquestrador) com `mat-stepper` de 4 passos e `ChangeDetectionStrategy.OnPush`
- [ ] 5.6 Implementar análise de query em tempo real (`POST /api/query/analyze`) no passo do editor
- [ ] 5.7 Implementar watchdog no cliente (10s sem eventos → connection lost)
- [ ] 5.8 Implementar double-cancel (AbortController + POST /cancel)

## 6. Tauri — Rust e IPC

- [ ] 6.1 Implementar spawn do sidecar `FurLab.Api` em `main.rs` com args `["--urls", "http://127.0.0.1:0"]`
- [ ] 6.2 Implementar parsing de stdout com regex para extrair URL da API
- [ ] 6.3 Emitir evento `api-ready` para o Angular com a URL descoberta
- [ ] 6.4 Implementar comando `get_api_url` via `invoke`
- [ ] 6.5 Implementar comando `show_in_folder(path)` com suporte Windows (`explorer /select,`) / macOS (`open -R`) / Linux (`xdg-open`)
- [ ] 6.6 Implementar comando `save_file_dialog(defaultName)` usando `FileDialogBuilder` do Tauri
- [ ] 6.7 Criar `src-tauri/capabilities/default.json` com permissões mínimas necessárias (filesystem, dialog, shell)
- [ ] 6.8 Configurar `tauri.conf.json` com `bundle.externalBin` apontando para `binaries/FurLab.Api`

## 7. Build e Scripts

- [ ] 7.1 Configurar `FurLab.Api.csproj` para publish self-contained (ou framework-dependent com .NET 10 runtime pré-instalado)
- [ ] 7.2 Criar `scripts/build-api.ps1` para build Release do `FurLab.Api` e copiar para `src-tauri/binaries/`
- [ ] 7.3 Criar `scripts/dev.ps1` para orquestrar 3 terminais (API + ng serve + tauri dev)
- [ ] 7.4 Criar `scripts/build-desktop.ps1` para build completo (dotnet → ng → cargo tauri build)
- [ ] 7.5 Renomear sidecar para formato `FurLab-x86_64-pc-windows-msvc.exe` no script de build
- [ ] 7.6 Testar build de debug com `cargo tauri build --debug`

## 8. Testes e Qualidade

- [ ] 8.1 Testar endpoint `GET /api/health` via curl
- [ ] 8.2 Testar endpoint `GET /api/servers` via curl
- [ ] 8.3 Testar streaming NDJSON via curl com `Accept: application/x-ndjson`
- [ ] 8.4 Testar cancelamento de execução (verificar se CancellationToken propaga para Parallel.ForEachAsync)
- [ ] 8.5 Testar `LocalhostOnlyMiddleware` acessando de outra máquina (deve retornar 403)
- [ ] 8.6 Testar fluxo de dev Fase 1: 3 terminais separados (API + ng serve + tauri dev)
- [ ] 8.7 Testar fluxo completo desktop: abrir app → escrever query → selecionar servers → executar → ver resultados
- [ ] 8.8 Testar "Open Folder" via Tauri IPC no Windows
- [ ] 8.9 Verificar bundle final do Tauri (tamanho, sidecar incluso)

## 9. Documentação e Entrega

- [ ] 9.1 Atualizar `README.md` com instruções de build da GUI
- [ ] 9.2 Documentar fluxo de desenvolvimento (Fase 1: 3 terminais / Fase 2: sidecar auto-spawn)
- [ ] 9.3 Verificar se `GUI_MIGRATION.md` está sincronizado com implementação final
- [ ] 9.4 Marcar change como pronta para archive no OpenSpec
