## Contexto

O FurLab possui um projeto `FurLab.Api` vazio (apenas pastas `bin/` e `obj/`) que não está vinculado à solução `.slnx`. Todo o motor de execução de queries reside hoje no `QueryRunCommand` do CLI — um monólito de ~876 linhas que mistura lógica de negócio, interação com console (Spectre.Console), paralelismo (`Parallel.ForEachAsync`), retry (Polly), descoberta de bancos e exportação CSV.

O objetivo é criar uma API ASP.NET Core local que exponha essa funcionalidade via endpoints HTTP e SSE, para consumo futuro por um frontend Angular. A API deve reutilizar a mesma configuração de servidores do CLI (`IUserConfigService`) e persistir resultados em disco no mesmo formato CSV.

## Goals / Non-Goals

**Goals:**
- Criar e vincular o projeto `FurLab.Api` à solução.
- Implementar endpoints REST/SSE para execução de queries: `analyze`, `execute`, `status`, `events` (SSE), `cancel`, `download`.
- Garantir que a API escute **apenas em localhost** (`127.0.0.1` / `::1`), rejeitando requisições externas.
- Extrair o motor de execução de queries do CLI para `FurLab.Core`, tornando-o reutilizável por CLI e API via abstrações (`IProgressObserverService`, `QueryPlannerService`, `QueryExecutorService`).
- Suportar múltiplas execuções simultâneas, cada uma com seu `executionId`.
- Enviar preview de até 50 linhas (ou 500KB) via SSE; o resultado completo permanece acessível apenas via download do CSV.
- Permitir reconexão do cliente Angular através do endpoint `GET /status` seguido de reconexão SSE.

**Non-Goals:**
- Autenticação/autorização (a API é local-only; não expõe interface pública).
- WebSockets (manter SSE por simplicidade).
- Modificar o formato dos arquivos CSV gerados.
- Modificar requisitos das capabilities de query-run existentes (a API é uma nova interface, não altera comportamento das specs existentes).
- Persistência histórica em banco de dados (execuções são efêmeras em memória; apenas os arquivos CSV em disco permanecem).

## Decisões

### 1. SSE em vez de WebSockets
- **Racional**: O fluxo é unidirecional (servidor → cliente) por natureza. SSE usa HTTP padrão, funciona nativamente em navegadores via `EventSource`, não requer handshake extra e é mais simples de implementar e depurar. WebSockets seria overkill.
- **Alternativa considerada**: WebSockets — rejeitada por adicionar complexidade sem benefício para um cenário puramente push.

### 2. Bind Kestrel em localhost + middleware de verificação de IP
- **Racional**: Defense in depth. O Kestrel é configurado para `ListenLocalhost(port)`, garantindo que o SO não aceite conexões em interfaces externas. O middleware `LocalhostSecurityMiddleware` verifica `RemoteIpAddress` e retorna 403 para qualquer IP não-loopback, protegendo contra túneis/proxy locais acidentais.
- **Alternativa considerada**: Apenas middleware — rejeitada porque não protege contra bind em todas as interfaces.

### 3. Preview de 50 linhas / 500KB no SSE
- **Racional**: O Angular precisa mostrar uma amostra dos dados em tempo real, mas não faz sentido transmitir datasets massivos por SSE. 50 linhas é suficiente para validar visualmente os dados; o cap de 500KB protege contra linhas muito largas (JSONB, textos). O dataset completo está sempre disponível via `GET /download`.
- **Alternativa considerada**: Enviar todas as linhas via SSE — rejeitada por poder matar a conexão e consumir memória excessiva.

### 4. Extração do motor de execução para `FurLab.Core`
- **Racional**: Eliminar duplicação de código entre CLI e API. Hoje o `QueryRunCommand` contém toda a lógica. Extraí-la para `QueryPlannerService` (descoberta de DBs), `QueryExecutorService` (paralelismo + retry) e `IProgressObserverService` (abstração de saída) permite que tanto o CLI (via `ConsoleObserverService`) quanto a API (via `SseEventSinkService`) compartilhem o mesmo motor.
- **Alternativa considerada**: Duplicar a lógica na API — rejeitada por inviabilizar manutenção.

### 5. `CsvExporterService` como sink secundário sempre ativo
- **Racional**: O CLI e a API devem produzir o mesmo artefato em disco. Ao tornar o `CsvExporterService` um sink independente do observer de UI, ambos os hosts (CLI e API) geram arquivos CSV idênticos sem acoplamento com a camada de apresentação.

### 6. `ExecutionRegistryService` com `ConcurrentDictionary<Guid, ExecutionContext>`
- **Racional**: Suportar múltiplas abas/executões simultâneas no Angular. Cada execução recebe um `Guid`, mantém seu `CancellationTokenSource` e seu estado. O registro permite cancelamento e consulta de status independentes.

### 7. Nomenclatura `*Service` para todas as classes de serviço
- **Racional**: Convenção já estabelecida no projeto (`ICredentialService`, `IUserConfigService`) e formalizada no `CLAUDE.md`. Todos os novos serviços de orquestração/infraestrutura devem terminar com `Service`.

## Risks / Trade-offs

| Risco | Mitigação |
|-------|-----------|
| Refactor do `QueryRunCommand` introduz regressões no CLI | Manter todos os testes existentes passando; criar testes de integração para o novo motor extraído antes de modificar o CLI. |
| SSE consome memória se o cliente não consome eventos rapidamente | Usar `Channel` com `BoundedChannelFullMode.Wait` e liberar referências do `ExecutionContext` após conclusão. |
| Execuções efêmeras em memória podem vazar se o cliente nunca reconecta | Implementar TTL no `ExecutionRegistryService` (ex: remover contextos concluídos após 30 min) ou limitar número máximo de execuções simultâneas. |
| Cancelamento de `Parallel.ForEachAsync` pode não ser imediato | Passar `CancellationToken` para todas as operações async (Npgsql, Polly) e verificar `ThrowIfCancellationRequested` em loops internos. |
| Múltiplas abas geram muitas conexões PostgreSQL simultâneas | Respeitar `MaxParallelism` do config do usuário; o `QueryExecutorService` deve usar o mesmo paralelismo configurado no CLI. |

## Migration Plan

1. Criar projeto `FurLab.Api` e adicionar à solução `.slnx`.
2. Extrair motor de execução para `FurLab.Core` (novos services e interfaces).
3. Refatorar `QueryRunCommand` para usar os novos services do Core.
4. Implementar controllers e middleware da API.
5. Validar que CLI continua funcionando (testes de regressão).
6. Testar endpoints da API via curl/HTTP client.

Não há migração de dados — a API é nova e não altera estado persistente além dos arquivos CSV já gerados pelo CLI.

## Open Questions

1. Qual porta padrão a API deve usar? (sugestão: `5000` ou dinâmica configurável)
2. Devemos adicionar um health-check endpoint (`/health`) para o Angular verificar se a API está no ar?
3. O `ExecutionRegistryService` deve remover execuções concluídas automaticamente após N minutos?
