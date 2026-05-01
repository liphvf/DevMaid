## Por que

O FurLab é hoje uma CLI robusta que executa queries SQL em múltiplos servidores PostgreSQL, gera CSVs e oferece feedback em tempo real via terminal. No entanto, a experiência de desenvolvedor poderia ser drasticamente melhorada com uma interface gráfica: seleção visual de servidores, editor SQL com syntax highlighting, progresso em tempo real em uma tabela ao vivo, e visualização consolidada de resultados. A stack anteriormente planejada (Electron) foi substituída por uma arquitetura mais performática (Tauri 2 + Angular 21 + .NET sidecar), mantendo o `FurLab.Core` existente e minimizando o bundle final (~5MB vs ~150MB). Este é um projeto "build to learn" com Angular 21.

## O que Muda

- **Nova capability**: Interface gráfica desktop para o comando `query run`, com 4 passos (Editor SQL → Seleção de Servidores → Execução com Streaming → Resultados Consolidados).
- **Nova capability**: API backend `FurLab.Api` (ASP.NET Core .NET 10) expondo REST + NDJSON streaming, consumindo `FurLab.Core` como sidecar do Tauri.
- **Novo projeto**: `src-ui/` com Angular 21, Angular Material, CodeMirror 6 e RxJS.
- **Novo projeto**: `src-tauri/` com Tauri 2 (Rust) como desktop shell, gerenciando lifecycle do sidecar .NET.
- **Novo script**: `scripts/build-desktop.ps1` para orquestrar build completo (`dotnet` → `ng` → `cargo tauri`).
- A CLI (`FurLab.CLI`) **permanece inalterada** e continua acessando `FurLab.Core` diretamente — não consome a API.

## Capabilities

### Novas Capacidades
- `desktop-gui-query-run`: Interface gráfica desktop para escrever queries SQL, selecionar servidores, executar com progresso em tempo real (streaming NDJSON) e visualizar resultados consolidados em tabela com filtro e paginação.
- `desktop-api`: API backend REST/NDJSON para servir a GUI desktop, incluindo endpoints de servidores, análise de query, execução com streaming, cancelamento e download de CSV.

### Capacidades Modificadas
- *(nenhuma — as specs existentes de query-run e settings não têm seus requisitos alterados; a GUI é um novo consumidor da mesma lógica de negócio)*

## Impacto

- **Projetos .NET**: Adição de `FurLab.Api/` ao `FurLab.slnx`; `FurLab.Core` reutilizado sem alterações.
- **Dependências frontend**: Node.js 24+ (LTS), Angular CLI 21, Angular Material 21, CodeMirror 6, RxJS.
- **Dependências desktop**: Tauri 2 CLI, Rust 1.95.0 toolchain, WebView2 Runtime (Windows).
- **Deployment**: O bundle desktop incluirá `FurLab.Api.exe` como sidecar empacotado pelo Tauri; modo web (`--web`) ficará para versões futuras.
