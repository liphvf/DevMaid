## Por que

O FurLab hoje só expõe suas funcionalidades via CLI interativo. Para suportar um frontend Angular no futuro, precisamos de uma API local que permita executar queries PostgreSQL com feedback em tempo real. Server-Sent Events (SSE) é a escolha ideal porque oferece streaming unidirecional simples sobre HTTP, eliminando a complexidade de WebSockets para um cenário onde o servidor apenas precisa notificar o cliente sobre progresso e resultados.

## O que Muda

- **ADICIONADO**: Novo projeto `FurLab.Api` (ASP.NET Core) vinculado à solução `.slnx`, configurado para escutar apenas em `localhost` (127.0.0.1/::1).
- **ADICIONADO**: Endpoints REST/SSE para execução de queries:
  - `POST /api/query/analyze` — analisa uma query SQL e retorna tipo, se é destrutiva e impacto estimado.
  - `POST /api/query/execute` — inicia uma execução e retorna `executionId`.
  - `GET /api/query/status?executionId={id}` — retorna estado atual da execução (para reconexão).
  - `GET /api/query/events?executionId={id}` — stream SSE com eventos de progresso, preview de resultados (máx. 50 linhas ou 500KB) e conclusão.
  - `POST /api/query/cancel?executionId={id}` — cancela uma execução em andamento.
  - `GET /api/query/download?executionId={id}&type={consolidated|server|log}` — retorna o arquivo CSV gerado em disco.
- **ADICIONADO**: Suporte a múltiplas execuções simultâneas via `ExecutionRegistryService`.
- **ADICIONADO**: `LocalhostSecurityMiddleware` que rejeita requisições não locais em duas camadas (bind Kestrel + verificação de IP).
- **MODIFICADO**: Extração do motor de execução de queries do `QueryRunCommand` (CLI) para `FurLab.Core`, introduzindo abstrações reutilizáveis (`IProgressObserverService`, `QueryPlannerService`, `QueryExecutorService`) para que CLI e API compartilhem a mesma lógica de negócio.
- **MODIFICADO**: `CsvExporterService` passa a ser um sink secundário compartilhado entre CLI e API, garantindo que ambos persistam resultados em disco no mesmo formato.

## Capacidades

### Novas Capacidades
- `local-api-query-sse`: API local para execução de queries PostgreSQL com streaming de progresso via SSE, permitindo múltiplas execuções simultâneas, cancelamento, reconexão e download de resultados CSV.

### Capacidades Modificadas
<!-- Nenhuma capability existente tem seus requisitos alterados; a API é uma nova interface para funcionalidades já especificadas. -->

## Impacto

- **Novo projeto**: `FurLab.Api/` com controllers, services e middleware.
- **Refactor em `FurLab.Core`**: extração da lógica de execução de queries do CLI para services reutilizáveis.
- **Refactor em `FurLab.CLI`**: `QueryRunCommand` passa a delegar para os novos services do Core.
- **Dependências**: ASP.NET Core (já presente no SDK .NET 10); nenhum pacote NuGet adicional necessário.
- **Configuração**: API reutiliza `IUserConfigService` e o mesmo arquivo de configuração do CLI (`%LocalAppData%/FurLab/settings.json`).
