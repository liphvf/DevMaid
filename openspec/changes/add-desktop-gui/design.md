## Contexto

O FurLab é uma CLI .NET 10 que automatiza tarefas de desenvolvimento, especialmente operações PostgreSQL. Em março de 2025, a camada `FurLab.Core` foi extraída do projeto monolítico original, separando a lógica de negócio da apresentação CLI. Esse trabalho preparou o terreno para uma interface gráfica, mas a implementação parou na fase 2 de 6.

Este design documenta a arquitetura para adicionar uma interface gráfica desktop ao FurLab, utilizando uma stack moderna e leve: **Tauri 2** como shell desktop, **Angular 21** como framework frontend, e **ASP.NET Core 10** como API backend que consome o `FurLab.Core` existente.

**Versões das dependências principais:**
- **.NET 10**: O projeto FurLab já utiliza `net10.0`. Se ainda estiver em preview/RC no momento da implementação, usar a última preview disponível.
- **Angular 21**: Última versão estável (released novembro 2025, active até maio 2026).
- **Tauri 2**: Última versão estável (2.x).
- **Rust 1.95.0**: Última versão estável do toolchain.
- **Node.js 24+**: Última versão LTS.

A comunicação entre frontend e backend utiliza **NDJSON over HTTP** (streaming de `IAsyncEnumerable` do ASP.NET Core 10), escolhido por ser mais leve que SignalR e mais eficiente que SSE/EventSource para o caso de uso unidirecional do FurLab.

**Nota importante sobre o Core:** A lógica de execução de query (`Parallel.ForEachAsync`, `Channel<CsvRow>`, retry com Polly) está atualmente em `FurLab.CLI.Commands.Query.Run.QueryRunCommand`. Para que a `FurLab.Api` a reutilize, é necessário extrair essa lógica para um novo serviço no `FurLab.Core` (ex: `IQueryExecutionService`). O `QueryRunCommand` da CLI passará a usar esse serviço, mantendo a compatibilidade total.

## Goals / Non-Goals

**Goals:**
- Criar interface gráfica desktop para o comando `query run` com 4 passos (Editor SQL → Seleção de Servidores → Execução com Streaming → Resultados Consolidados).
- Criar API backend `FurLab.Api` (ASP.NET Core .NET 10) expondo REST + NDJSON streaming.
- Empacotar a aplicação como desktop app via Tauri 2 com sidecar .NET auto-spawn.
- Manter a CLI existente inalterada — ela continua acessando `FurLab.Core` diretamente.
- Garantir que a API só seja acessível via localhost (bind em `127.0.0.1` + middleware de verificação).

**Non-Goals:**
- Modificar comandos CLI existentes para consumir a API.
- Implementar GUI para outros comandos (backup, restore, winget, etc.) — ficam para versões futuras.
- Implementar modo web (`fur gui --web`) no MVP.
- Implementar auto-updater do Tauri no MVP.
- Implementar CRUD de servidores na GUI — a GUI apenas lê a configuração existente.
- Adicionar autenticação/JWT na API — o uso é exclusivamente local.

## Decisions

### 1. Tauri 2 em vez de Electron
**Decisão:** Utilizar Tauri 2 como framework desktop em vez do Electron previamente planejado.

**Rationale:**
- **Bundle size:** ~5MB (Tauri) vs ~150MB (Electron).
- **Memória:** Tauri usa a WebView nativa do SO (Edge WebView2 no Windows, WKWebView no macOS, WebKitGTK no Linux) em vez de embutir um navegador completo.
- **Performance de startup:** Significativamente mais rápido.
- **Segurança:** Modelo de sandbox do Tauri é mais restritivo por padrão. Tauri 2 utiliza o novo sistema de capabilities baseado em arquivos JSON (`src-tauri/capabilities/`) em vez da allowlist embutida no `tauri.conf.json`.

**Alternativas consideradas:**
- Electron 41: Rejeitado devido ao tamanho do bundle e consumo de memória.
- Flutter: Rejeitado pois o projeto é "build to learn" com Angular.
- .NET MAUI: Rejeitado pois a integração com Angular não é natural e o ecossistema de componentes UI é inferior ao Angular Material.

### 2. NDJSON em vez de SSE ou SignalR
**Decisão:** Utilizar NDJSON (Newline Delimited JSON) over HTTP para streaming de eventos da API para o Angular.

**Rationale:**
- **Unidirecionalidade:** O FurLab só precisa de streaming servidor→cliente. NDJSON é perfeito para isso.
- **Suporte nativo ASP.NET Core 10:** `IAsyncEnumerable<T>` serializa automaticamente como NDJSON quando o client solicita `application/x-ndjson`.
- **Menor bundle:** Sem dependência `@microsoft/signalr` (~50KB+).
- **Proxy-friendly:** Funciona sobre HTTP/1.1 sem upgrade para WebSocket, evitando problemas em redes corporativas.
- **Controle total no cliente:** Usa `fetch()` + `ReadableStream` em vez de `EventSource`, permitindo headers customizados e cancelamento fino.

**Alternativas consideradas:**
- SSE (text/event-stream): Rejeitado pois requer prefixo `data:` em cada linha e parsing manual no cliente. NDJSON é mais eficiente.
- SignalR: Rejeitado pois é overkill para streaming unidirecional. Adiciona complexidade de Hub, Groups, Connection management.
- gRPC + gRPC-Web: Rejeitado pois requer proxy (Envoy) ou server-reflection, adicionando infraestrutura desnecessária para app local.

### 3. CodeMirror 6 em vez de Monaco Editor
**Decisão:** Utilizar CodeMirror 6 como editor SQL no Angular.

**Rationale:**
- **Bundle size:** ~200KB (CodeMirror 6 + lang-sql) vs ~3-5MB (Monaco).
- **Suficiência:** Para SQL, syntax highlighting e autocompletion básica de keywords são suficientes. Monaco brilha em features avançadas (IntelliSense, Go to Definition) que não são necessárias para queries SQL ad-hoc.
- **Integração Angular:** CodeMirror 6 é mais "web-native" e integra-se melhor com o ciclo de vida do Angular.

**Alternativas consideradas:**
- Monaco Editor: Rejeitado devido ao tamanho do bundle e complexidade de configuração (workers, lazy loading).
- Textarea simples: Rejeitado pois syntax highlighting melhora significativamente a UX.

### 4. Porta aleatória + stdout parsing para descoberta
**Decisão:** O Kestrel usa porta `0` (aleatória) e informa a porta efetiva via stdout. O Tauri parseia o stdout para descobrir a URL da API.

**Rationale:**
- **Evita conflitos:** Porta fixa poderia estar em uso por outro processo.
- **Idiomático:** Tauri 2 tem suporte nativo a sidecars e stdout capture.
- **Sem registry/config:** Não requer escrever em registry do Windows ou arquivos de configuração compartilhados.

**Alternativas consideradas:**
- Porta fixa (ex: 5000): Rejeitado devido ao risco de conflito.
- Arquivo temporário (`%TEMP%/furlab-api-port.txt`): Considerado como fallback, mas stdout parsing é mais elegante.

### 5. BehaviorSubject em vez de NgRx
**Decisão:** Utilizar RxJS `BehaviorSubject` em services singleton para state management, sem NgRx/Akita/Redux.

**Rationale:**
- **Dentro do framework:** `BehaviorSubject` é RxJS, que já é dependência obrigatória do Angular.
- **Simplicidade:** Para o escopo do MVP (uma única feature com fluxo linear), NgRx adiciona boilerplate desnecessário (Actions, Reducers, Effects, Selectors).
- **Performance:** Com `ChangeDetectionStrategy.OnPush`, a performance é equivalente.

**Alternativas consideradas:**
- NgRx: Rejeitado pois adiciona ~50KB de bundle e complexidade de boilerplate para um escopo pequeno.
- Akita: Rejeitado pois é biblioteca externa não mantida pela equipe do Angular.

### 6. CLI permanece direta (não consome API)
**Decisão:** A `FurLab.CLI` continua acessando `FurLab.Core` diretamente. Não consome a `FurLab.Api`.

**Rationale:**
- **Não quebrar o que funciona:** A CLI é o produto principal e está estável.
- **Startup:** Adicionar HTTP à CLI aumentaria o tempo de startup em ~500ms-1s (subir Kestrel).
- **Escopo:** O MVP é a GUI, não a unificação da arquitetura.

**Alternativas consideradas:**
- CLI como thin client HTTP: Rejeitado para o MVP, mas pode ser reconsiderado no futuro se surgir necessidade (ex: VS Code Extension, modo servidor).

### 7. Download CSV híbrido (API + IPC)
**Decisão:** No desktop, usar Tauri IPC para copiar o arquivo diretamente para a pasta escolhida pelo usuário. No modo web (futuro), usar download HTTP via API.

**Rationale:**
- **Performance:** IPC evita serialização HTTP e carregamento do arquivo em memória do browser.
- **UX nativa:** Save As dialog nativo do SO.
- **Compatibilidade futura:** A abstração permite funcionar em modo web quando implementado.

## Risks / Trade-offs

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| **WebView2 não instalado no Windows** | Alto em ambientes corporativos lockdown | O installer do Tauri pode embutir o WebView2 Runtime. Documentar pré-requisito. |
| **Sidecar .NET não inicia** | Alto — app fica sem backend | Tauri deve detectar falha de spawn e mostrar erro claro. Timeout de 10s para descoberta de porta. |
| **Porta aleatória bloqueada por firewall** | Médio | Como é localhost, firewalls geralmente permitem. Documentar exceção necessária. |
| **Bundle .NET self-contained é grande** | Médio (~30-50MB) | Aceitável comparado aos 150MB+ do Electron. Futuramente, pode-se usar .NET trimmed. |
| **Angular Material Table lenta com +10k rows** | Médio | Implementar `cdk-virtual-scroll` se necessário. Para MVP, paginação de 50 rows é suficiente. |
| **NDJSON não reconecta automaticamente** | Baixo | O SSE tem reconexão nativa; NDJSON requer retry manual. Como o FurLab é local, reconexão é raramente necessária. Implementar retry simples no `QueryService`. |
| **Múltiplas stacks (Rust + TS + C#) aumentam curva de aprendizado** | Médio | Documentar bem. A parte Rust é mínima (~100 linhas). O foco de desenvolvimento é TS + C#. |

## Migration Plan

**Deploy:**
1. Build `FurLab.Api` em Release.
2. Build Angular em produção (`ng build --configuration production`).
3. Build Tauri (`cargo tauri build`), que empacota o Angular estático + sidecar .NET.
4. Distribuir o installer `.msi` (Windows) gerado pelo Tauri.

**Rollback:**
- A CLI não é afetada. Usuários podem continuar usando `fur query run` normalmente.
- A GUI é um executável separado (`FurLab.exe`). Remover o atalho/desktop é suficiente para "rollback".

**Pré-requisitos do usuário:**
- Windows 10+ com WebView2 Runtime (instalado por padrão no Windows 11, disponível no 10).
- .NET 10 Runtime NÃO é necessário se o sidecar for publicado como self-contained.

## Open Questions

1. **Devemos implementar um heartbeat/ping no NDJSON?** O watchdog no cliente (10s sem eventos) pode ser suficiente, mas um ping explícito a cada 5s do servidor torna a detecção de crash mais robusta.
2. **Qual o limite de rows que a Material Table deve suportar antes de exigir virtual scroll?** 1000? 5000? Isso afeta a decisão de implementar `cdk-virtual-scroll` no MVP.
3. **Devemos incluir um header secreto (X-FurLab-Secret) além do bind localhost?** Adiciona segurança extra mas aumenta a complexidade do spawn do sidecar.
4. **O modo `fur gui --web` deve ser implementado junto ou separado?** Tecnicamente é trivial (só apontar navegador para localhost), mas adiciona escopo de teste.
