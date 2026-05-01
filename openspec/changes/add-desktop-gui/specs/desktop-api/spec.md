## ADDED Requirements

### Requirement: Health check da API
O sistema DEVE expor endpoint de health check para verificação de disponibilidade.

#### Scenario: API está saudável
- **QUANDO** o cliente faz `GET /api/health`
- **ENTÃO** o sistema retorna HTTP 200 com body `{ "status": "healthy", "version": "1.0.0" }`

---

### Requirement: Bind em localhost apenas
O sistema DEVE vincular o servidor Kestrel exclusivamente ao endereço de loopback.

#### Scenario: Inicialização da API
- **QUANDO** a API é iniciada
- **ENTÃO** o Kestrel escuta em `http://127.0.0.1:0` (porta aleatória)
- **E** NÃO escuta em `0.0.0.0` ou interfaces externas

---

### Requirement: Middleware de bloqueio de acesso remoto
O sistema DEVE rejeitar requisições que não originem do loopback.

#### Scenario: Requisição de localhost é aceita
- **QUANDO** uma requisição chega de `127.0.0.1` ou `::1`
- **ENTÃO** a requisição é processada normalmente

#### Scenario: Requisição remota é bloqueada
- **QUANDO** uma requisição chega de um IP não-loopback
- **ENTÃO** o sistema responde com HTTP 403 "Forbidden: local only"

---

### Requirement: Descoberta de porta via stdout
O sistema DEVE informar a porta efetiva de escuta via stdout para que o sidecar Tauri possa descobri-la.

#### Scenario: Log de porta no startup
- **QUANDO** o Kestrel inicia em uma porta aleatória
- **ENTÃO** o sistema escreve no stdout: `[FurLab.Api] Listening on http://127.0.0.1:{porta}`

---

### Requirement: Listagem de servidores configurados
O sistema DEVE expor endpoint REST para listar servidores do arquivo de configuração do usuário.

#### Scenario: Listagem bem-sucedida
- **QUANDO** o cliente faz `GET /api/servers`
- **ENTÃO** o sistema retorna array JSON com nome, host, port, databases e fetchAllDatabases

---

### Requirement: Análise de query SQL
O sistema DEVE expor endpoint para análise estática de queries SQL.

#### Scenario: Análise de SELECT
- **QUANDO** o cliente faz `POST /api/query/analyze` com body `{ "sql": "SELECT 1" }`
- **ENTÃO** o sistema retorna `{ "type": "SELECT", "isDestructive": false, "tables": [], "estimatedCost": "low" }`

#### Scenario: Análise de DELETE
- **QUANDO** o cliente faz `POST /api/query/analyze` com body `{ "sql": "DELETE FROM users" }`
- **ENTÃO** o sistema retorna `{ "type": "DELETE", "isDestructive": true, ... }`

---

### Requirement: Preview de databases
O sistema DEVE expor endpoint para preview de databases que serão afetados sem executar a query.

#### Scenario: Preview com fetchAll
- **QUANDO** o cliente faz `POST /api/query/preview` com `fetchAllDatabases: true`
- **ENTÃO** o sistema conecta aos servidores, descobre databases via `pg_database`, e retorna lista por servidor

---

### Requirement: Execução de query com streaming NDJSON
O sistema DEVE executar queries em múltiplos servidores/databases e streamar resultados em formato NDJSON.

#### Scenario: Streaming iniciado
- **QUANDO** o cliente faz `GET /api/query/execute` com parâmetros sql, servers e fetchAll
- **E** header `Accept: application/x-ndjson`
- **ENTÃO** o sistema retorna HTTP 200 com `Content-Type: application/x-ndjson`
- **E** inicia o stream de eventos

#### Scenario: Evento de início
- **QUANDO** a execução começa
- **ENTÃO** o sistema emite linha NDJSON: `{"type":"start","sessionId":"...","servers":2,"totalDatabases":12}`

#### Scenario: Evento de discovery
- **QUANDO** um servidor tem databases descobertos
- **ENTÃO** o sistema emite: `{"type":"db_discovered","server":"prod-01","databases":["db1","db2"]}`

#### Scenario: Evento de progresso
- **QUANDO** uma execução em um database inicia
- **ENTÃO** o sistema emite: `{"type":"progress","server":"...","database":"...","currentStep":3,"totalSteps":12}`

#### Scenario: Evento de resultado
- **QUANDO** uma execução em um database completa
- **ENTÃO** o sistema emite: `{"type":"result","server":"...","database":"...","status":"success","rows":450,"durationMs":1200}`

#### Scenario: Evento de erro com retry
- **QUANDO** uma execução falha e haverá retry
- **ENTÃO** o sistema emite: `{"type":"error","server":"...","database":"...","error":"timeout","willRetry":true,"retryAttempt":1,"maxRetries":3}`

#### Scenario: Evento de retry
- **QUANDO** um retry é iniciado
- **ENTÃO** o sistema emite: `{"type":"retry","server":"...","database":"...","retryAttempt":2,"maxRetries":3,"nextDelayMs":1000}`

#### Scenario: Evento de completion
- **QUANDO** todas as execuções terminam
- **ENTÃO** o sistema emite: `{"type":"complete","successCount":11,"failureCount":1,"totalRows":2340,"outputDirectory":"..."}`
- **E** fecha o stream

---

### Requirement: Cancelamento de execução
O sistema DEVE permitir cancelamento de uma execução ativa via endpoint.

#### Scenario: Cancelamento bem-sucedido
- **QUANDO** o cliente faz `POST /api/query/cancel/{sessionId}`
- **ENTÃO** o sistema sinaliza CancellationTokenSource associado à sessão
- **E** propaga o cancelamento para o `Parallel.ForEachAsync` em execução
- **E** retorna HTTP 204

#### Scenario: Cancelamento após conclusão
- **QUANDO** o cliente faz `POST /api/query/cancel/{sessionId}` de uma sessão já finalizada
- **ENTÃO** o sistema retorna HTTP 404

---

### Requirement: Download de CSV
O sistema DEVE servir arquivos CSV gerados pela execução de query.

#### Scenario: Download consolidado
- **QUANDO** o cliente faz `GET /api/query/download/{sessionId}?type=consolidated`
- **ENTÃO** o sistema retorna o arquivo `consolidado_{timestamp}.csv` com `Content-Type: text/csv`

#### Scenario: Download de erros
- **QUANDO** o cliente faz `GET /api/query/download/{sessionId}?type=errors`
- **ENTÃO** o sistema retorna o arquivo `erros_{timestamp}.csv`

#### Scenario: Download de log
- **QUANDO** o cliente faz `GET /api/query/download/{sessionId}?type=log`
- **ENTÃO** o sistema retorna o arquivo `log_{timestamp}.csv`

#### Scenario: Sessão não encontrada
- **QUANDO** o cliente solicita download de uma sessão inexistente
- **ENTÃO** o sistema retorna HTTP 404
