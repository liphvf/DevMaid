# Plano de Implementação: Interface Gráfica — Electron + Angular

**Spec:** [010-gui-electron-angular](./spec.md)  
**Status:** Planejado  
**Estimativa Total:** ~11 semanas  

---

## Visão Geral das Fases

| Fase | Nome | Duração Estimada | Status | Pré-requisito |
|------|------|-----------------|--------|---------------|
| 1 | Extração da camada Core | — | **Concluído** | — |
| 2 | Refatoração da CLI para usar Core | — | **Concluído** | Fase 1 |
| 3 | Backend `DevMaid.Api` | ~2 semanas | Planejado | Fase 2 |
| 4 | Frontend Angular | ~3 semanas | Planejado | Fase 3 |
| 5 | Integração Electron + empacotamento | ~1 semana | Planejado | Fase 4 |
| 6 | Comando híbrido `devmaid gui` | ~1 semana | Planejado | Fase 5 |

---

## Fase 1 — Extração da Camada Core

> **Status: Concluído**

`DevMaid.Core` existe com interfaces e serviços completos:

- `DevMaid.Core/Interfaces/` — 5 interfaces (`IDatabaseService`, `IFileService`, `IWingetService`, `IProcessExecutor`, `IConfigurationService`)
- `DevMaid.Core/Services/` — 7 implementações concretas
- `DevMaid.Core/Models/` — modelos de dados compartilhados

---

## Fase 2 — Refatoração da CLI

> **Status: Concluído**

`DevMaid.CLI` usa `services.AddDevMaidServices()` registrado em `DevMaid.Core`. Classes em `DevMaid.CLI/Services/` são wrappers de compatibilidade que delegam para `DevMaid.Core`.

---

## Fase 3 — Backend `DevMaid.Api`

**Objetivo:** Criar a camada de API REST + SignalR que a GUI consumirá.

### Tarefas

- [ ] **3.1** Criar projeto `DevMaid.Api` (ASP.NET Core Web API) na solução `DevMaid.slnx`
- [ ] **3.2** Adicionar referência a `DevMaid.Core`
- [ ] **3.3** Registrar `AddDevMaidServices()` no `Program.cs` da API
- [ ] **3.4** Implementar `DatabaseController` (backup, restore, list-databases)
- [ ] **3.5** Implementar `FileController` (combine)
- [ ] **3.6** Implementar `WingetController` (backup, restore)
- [ ] **3.7** Implementar `QueryController` (run-query, export-csv)
- [ ] **3.8** Implementar `CleanController` (clean)
- [ ] **3.9** Implementar `WindowsFeaturesController` (list, enable, disable)
- [ ] **3.10** Implementar `ConfigurationController` (get, update — lê/grava `appsettings.json`)
- [ ] **3.11** Adicionar suporte a SignalR: criar `OperationHub` para progresso em tempo real
- [ ] **3.12** Criar `SignalRProgressReporter : IProgress<OperationProgress>` para integrar serviços Core com SignalR
- [ ] **3.13** Configurar CORS para aceitar apenas `app://*` (origem Electron)
- [ ] **3.14** Configurar bind exclusivo em `localhost` (127.0.0.1) com porta configurável (padrão: `5299`)
- [ ] **3.15** Adicionar rate limiting (100 req/min por IP, janela deslizante)
- [ ] **3.16** Adicionar OpenAPI/Swagger para documentação dos endpoints
- [ ] **3.17** Escrever testes de integração para cada controller em `DevMaid.Tests/Integration/Api/`

### Decisões Técnicas

| Decisão | Escolha | Justificativa |
|---------|---------|---------------|
| Transporte de progresso | SignalR | Suporte nativo Angular, tempo real sem polling |
| Comunicação frontend-backend | REST + SignalR | REST para operações; SignalR para progresso |
| Autenticação inicial | Nenhuma (bind localhost) | Superfície de ataque mínima; credenciais nunca expostas na rede |
| Porta padrão | `5299` | Não conflita com portas comuns de desenvolvimento |

---

## Fase 4 — Frontend Angular

**Objetivo:** Criar interface visual moderna para todas as features da CLI.

### Tarefas

- [ ] **4.1** Criar projeto Angular 18+ em `DevMaid.Gui/angular-app/`
- [ ] **4.2** Configurar Angular Material + tema DevMaid
- [ ] **4.3** Criar estrutura de módulos: `core/`, `shared/`, `features/`, `layout/`
- [ ] **4.4** Criar serviço `ApiService` (HttpClient wrapper com interceptors)
- [ ] **4.5** Criar serviço `SignalRService` (subscription de progresso em tempo real)
- [ ] **4.6** Criar componentes compartilhados: `ProgressDialogComponent`, `ErrorDialogComponent`, `ConfirmationDialogComponent`
- [ ] **4.7** Criar layout principal: `SidebarComponent`, `HeaderComponent`
- [ ] **4.8** Implementar módulo **Database**: backup, restore, list-databases
- [ ] **4.9** Implementar módulo **File**: combine
- [ ] **4.10** Implementar módulo **Winget**: backup, restore
- [ ] **4.11** Implementar módulo **Query**: executar query, exportar CSV
- [ ] **4.12** Implementar módulo **Clean**: limpar projetos .NET
- [ ] **4.13** Implementar módulo **Windows Features**: listar, habilitar, desabilitar
- [ ] **4.14** Implementar tela de **Configurações** (lê/grava `appsettings.json` via API)
- [ ] **4.15** Implementar persistência de última configuração por operação
- [ ] **4.16** Garantir score ≥ 90 na auditoria de acessibilidade do Lighthouse
- [ ] **4.17** Escrever testes E2E com Playwright para os fluxos críticos (backup de banco, restore de banco)

---

## Fase 5 — Integração Electron + Empacotamento

**Objetivo:** Empacotar a aplicação Angular como aplicativo desktop Windows.

### Tarefas

- [ ] **5.1** Criar processo principal Electron em `DevMaid.Gui/electron/main.ts`
- [ ] **5.2** Criar preload script com `contextBridge` (expõe apenas `electronAPI` definida — sem `nodeIntegration`)
- [ ] **5.3** Configurar `BrowserWindow` para carregar a build de produção do Angular
- [ ] **5.4** Implementar handlers IPC: `get-app-version`, `minimize-window`, `maximize-window`, `close-window`
- [ ] **5.5** Configurar `electron-builder` para gerar instalador NSIS (Windows)
- [ ] **5.6** Configurar ícone (`icon.ico`) e metadados do instalador (`appId: com.devmaid.gui`)
- [ ] **5.7** Validar que o instalador empacotado tem menos de 200 MB
- [ ] **5.8** Validar que a inicialização (de `devmaid gui` até janela interativa) ocorre em menos de 5 segundos no hardware alvo

### Requisitos de Segurança do Electron

- `nodeIntegration: false`
- `contextIsolation: true`
- Nenhuma API `node` exposta diretamente ao renderer

---

## Fase 6 — Comando Híbrido `devmaid gui`

**Objetivo:** Integrar o lançamento da GUI ao binário CLI existente.

### Tarefas

- [ ] **6.1** Adicionar `GUICommand` em `DevMaid.CLI/Commands/GUICommand.cs`
- [ ] **6.2** Registrar `GUICommand.Build()` em `DevMaid.CLI/Program.cs`
- [ ] **6.3** `GUICommand` deve: iniciar `DevMaid.Api` em segundo plano → lançar `DevMaid.Gui.exe` → aguardar fechamento da GUI → encerrar a API
- [ ] **6.4** Implementar detecção de instância única (evitar múltiplas instâncias de API/GUI simultâneas)
- [ ] **6.5** Fechar a GUI também deve encerrar o servidor API em segundo plano (CA-010.6)
- [ ] **6.6** Sair com código `0` em fechamento normal; `1` se a API falhar ao iniciar ou se o binário da GUI não for encontrado (tabela de códigos de saída em `spec.md`)
- [ ] **6.7** Atualizar `README.md` e `README.pt-BR.md` com documentação do comando `devmaid gui`

---

## Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Processos longos bloqueando a API | Média | Alto | `async/await` em toda a stack; `IProgress<T>` via SignalR |
| Quebra de compatibilidade CLI após mudanças no Core | Baixa | Alto | Testes de integração obrigatórios antes de merge |
| Tamanho do instalador > 200 MB | Média | Médio | Monitorar tamanho do bundle Angular; usar `tree-shaking` agressivo |
| Inicialização > 5 segundos | Baixa | Médio | Lazy loading de módulos Angular; pré-aquecimento da API |

---

## Critérios de Conclusão da Feature

A feature 010 estará **Implementada** quando:

1. Todos os critérios de aceitação `CA-010.1` a `CA-010.7` passarem
2. O instalador NSIS estiver publicado como artefato de release
3. Os testes E2E de backup e restore de banco passarem no CI
4. A auditoria de acessibilidade do Lighthouse marcar ≥ 90
5. O tamanho do instalador for < 200 MB
6. A inicialização for < 5 segundos no hardware alvo
