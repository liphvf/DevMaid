## ADICIONADOS Requisitos

### Requisito: API local-only para execução de queries
A API DEVE escutar apenas em interfaces de loopback (`127.0.0.1` e `::1`) e DEVE rejeitar requisições cujo endereço de origem não seja local.

#### Cenário: Requisição local aceita
- **QUANDO** o Angular envia uma requisição HTTP para `localhost:{porta}`
- **ENTÃO** a API processa a requisição normalmente

#### Cenário: Requisição externa rejeitada
- **QUANDO** uma requisição chega de um IP não-loopback
- **ENTÃO** a API retorna HTTP 403 Forbidden antes de processar qualquer lógica de negócio

### Requisito: Endpoint de análise de query
O sistema DEVE expor `POST /api/query/analyze` que recebe uma query SQL e retorna o tipo de query, se é destrutiva e o impacto estimado.

#### Cenário: Análise de query SELECT
- **QUANDO** o cliente envia `{ "sql": "SELECT * FROM users" }`
- **ENTÃO** a resposta DEVE conter `"isDestructive": false`, `"queryType": "SELECT"` e `"requiresConfirmation": false`

#### Cenário: Análise de query DELETE
- **QUANDO** o cliente envia `{ "sql": "DELETE FROM users WHERE inactive = true" }`
- **ENTÃO** a resposta DEVE conter `"isDestructive": true`, `"queryType": "DELETE"` e `"requiresConfirmation": true`

### Requisito: Endpoint de execução de query
O sistema DEVE expor `POST /api/query/execute` que inicia a execução de uma query em um ou mais servidores e retorna um `executionId` único.

#### Cenário: Execução iniciada com sucesso
- **QUANDO** o cliente envia uma requisição válida com SQL, servidores e opções
- **ENTÃO** a API DEVE retornar `201 Created` com `executionId`, `status: "started"` e `outputDirectory`

#### Cenário: Query destrutiva sem confirmação
- **QUANDO** o cliente envia uma query destrutiva sem `confirmed: true`
- **ENTÃO** a API DEVE retornar `400 Bad Request` com mensagem indicando que confirmação é necessária

### Requisito: Stream SSE de eventos de execução
O sistema DEVE expor `GET /api/query/events?executionId={id}` que retorna um stream SSE com eventos de progresso, preview de resultados e conclusão.

#### Cenário: Evento de início de execução
- **QUANDO** uma execução é iniciada
- **ENTÃO** o primeiro evento SSE DEVE ser `event: execution-started` com `executionId`, `servers`, `databases` e `timestamp`

#### Cenário: Evento de query em execução
- **QUANDO** uma query começa a rodar em um banco de dados
- **ENTÃO** o SSE DEVE emitir `event: query-executing` com `server`, `database` e `timestamp`

#### Cenário: Evento de query concluída com preview
- **QUANDO** uma query conclui com sucesso
- **ENTÃO** o SSE DEVE emitir `event: query-completed` contendo `server`, `database`, `rowCount`, `columns` e `preview` com até 50 linhas ou 500KB
- **E** o campo `previewTruncated` DEVE ser `true` se o limite for atingido

#### Cenário: Evento de falha em query
- **QUANDO** uma query falha em um banco de dados
- **ENTÃO** o SSE DEVE emitir `event: query-failed` contendo `server`, `database` e `error`

#### Cenário: Evento de execução concluída
- **QUANDO** todas as queries de uma execução terminam
- **ENTÃO** o SSE DEVE emitir `event: execution-completed` com `successCount`, `failureCount`, `totalRows` e `outputPath`

### Requisito: Endpoint de status para reconexão
O sistema DEVE expor `GET /api/query/status?executionId={id}` que retorna o estado atual de uma execução, permitindo reconexão do cliente.

#### Cenário: Consulta de execução em andamento
- **QUANDO** o cliente consulta o status de uma execução ativa
- **ENTÃO** a resposta DEVE conter `status: "running"`, `progress` com `totalDatabases`, `completed`, `failed`, `inProgress`, `startedAt` e lista parcial de `results`

#### Cenário: Consulta de execução concluída
- **QUANDO** o cliente consulta o status de uma execução finalizada
- **ENTÃO** a resposta DEVE conter `status: "completed"`, `completedAt`, `outputDirectory` e todos os `results`

#### Cenário: Execução não encontrada
- **QUANDO** o cliente consulta um `executionId` inexistente ou expirado
- **ENTÃO** a API DEVE retornar `404 Not Found`

### Requisito: Endpoint de cancelamento
O sistema DEVE expor `POST /api/query/cancel?executionId={id}` que cancela uma execução em andamento.

#### Cenário: Cancelamento com sucesso
- **QUANDO** o cliente solicita cancelamento de uma execução em andamento
- **ENTÃO** a API DEVE sinalizar cancelamento para todas as operações paralelas em execução
- **E** DEVE retornar `200 OK` com `status: "cancelled"`

#### Cenário: Cancelamento de execução já concluída
- **QUANDO** o cliente solicita cancelamento de uma execução já finalizada
- **ENTÃO** a API DEVE retornar `409 Conflict` com mensagem indicando que a execução já terminou

### Requisito: Endpoint de download de resultados
O sistema DEVE expor `GET /api/query/download?executionId={id}&type={tipo}` que retorna o arquivo CSV gerado durante a execução.

#### Cenário: Download do CSV consolidado
- **QUANDO** o cliente solicita download com `type=consolidated`
- **ENTÃO** a API DEVE retornar o arquivo `consolidado_{timestamp}.csv` com `Content-Type: text/csv`

#### Cenário: Download de log de execução
- **QUANDO** o cliente solicita download com `type=log`
- **ENTÃO** a API DEVE retornar o arquivo `{timestamp}_log.csv` com `Content-Type: text/csv`

#### Cenário: Arquivo não encontrado
- **QUANDO** o cliente solicita download de uma execução inexistente ou sem arquivos
- **ENTÃO** a API DEVE retornar `404 Not Found`

### Requisito: Motor de execução compartilhado entre CLI e API
O sistema DEVE extrair a lógica de execução de queries do `QueryRunCommand` para services reutilizáveis em `FurLab.Core`, permitindo que CLI e API compartilhem o mesmo motor.

#### Cenário: CLI utiliza motor extraído
- **QUANDO** o usuário executa `fur query run` no CLI
- **ENTÃO** o `QueryRunCommand` DEVE delegar para `QueryPlannerService` e `QueryExecutorService`
- **E** o resultado em disco DEVE ser idêntico ao formato anterior

#### Cenário: API utiliza o mesmo motor
- **QUANDO** o cliente chama `POST /api/query/execute`
- **ENTÃO** a API DEVE utilizar `QueryPlannerService` e `QueryExecutorService`
- **E** o resultado em disco DEVE seguir o mesmo formato CSV do CLI

### Requisito: Múltiplas execuções simultâneas
O sistema DEVE suportar múltiplas execuções de query ao mesmo tempo, cada uma com identificador único e estado independente.

#### Cenário: Duas execuções simultâneas
- **QUANDO** o cliente inicia duas execuções de query diferentes em sequência rápida
- **ENTÃO** a API DEVE gerar dois `executionId` distintos
- **E** ambas as execuções DEVEm progredir independentemente sem interferência
