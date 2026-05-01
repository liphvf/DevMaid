## ADDED Requirements

### Requirement: Editor SQL com syntax highlighting
O sistema DEVE fornecer um editor de texto para escrita de queries SQL com syntax highlighting.

#### Scenario: Usuário digita uma query
- **QUANDO** o usuário acessa a tela de Query Run
- **ENTÃO** o sistema exibe um editor CodeMirror 6 com suporte à linguagem SQL

#### Scenario: Syntax highlighting é aplicado
- **QUANDO** o usuário digita `SELECT * FROM pg_database`
- **ENTÃO** as palavras-chave SQL (`SELECT`, `FROM`) são destacadas visualmente

---

### Requirement: Análise de query em tempo real
O sistema DEVE analisar a query enquanto o usuário digita e exibir metadados sobre tipo e risco.

#### Scenario: Query não-destrutiva é detectada
- **QUANDO** o usuário digita `SELECT 1`
- **ENTÃO** o sistema exibe indicador "Query Type: SELECT" e "Destructive: NO"

#### Scenario: Query destrutiva é detectada
- **QUANDO** o usuário digita `DELETE FROM users`
- **ENTÃO** o sistema exibe indicador "Destructive: YES" com alerta visual

---

### Requirement: Seleção visual de servidores
O sistema DEVE exibir todos os servidores configurados e permitir seleção múltipla com checkboxes.

#### Scenario: Servidores são listados
- **QUANDO** o usuário avança para o passo de seleção de servidores
- **ENTÃO** o sistema exibe a lista de servidores obtida via `GET /api/servers`

#### Scenario: Todos os servidores pré-selecionados
- **QUANDO** a lista de servidores é carregada
- **ENTÃO** todos os servidores vêm pré-selecionados por padrão

#### Scenario: Seleção individual
- **QUANDO** o usuário desmarca um servidor
- **ENTÃO** o servidor removido não será incluído na execução

---

### Requirement: Preview de databases afetadas
O sistema DEVE permitir preview do número estimado de databases que serão consultados.

#### Scenario: Preview com databases configurados
- **QUANDO** o usuário seleciona servidores sem "Fetch all databases"
- **ENTÃO** o sistema exibe o número de databases configurados por servidor

#### Scenario: Preview com auto-discovery
- **QUANDO** o usuário ativa "Fetch all databases"
- **ENTÃO** o sistema exibe indicativo de que o número real será descoberto na execução

---

### Requirement: Execução com streaming em tempo real
O sistema DEVE executar a query em todos os servidores/databases selecionados e exibir progresso em tempo real via streaming NDJSON.

#### Scenario: Início da execução
- **QUANDO** o usuário clica em "Run Query"
- **ENTÃO** o sistema inicia o streaming NDJSON via `GET /api/query/execute`
- **E** exibe barra de progresso geral

#### Scenario: Progresso de execução por database
- **QUANDO** o servidor envia evento `progress`
- **ENTÃO** o sistema atualiza a tabela ao vivo com o status do par (servidor, database)

#### Scenario: Resultado de sucesso
- **QUANDO** o servidor envia evento `result` com status `success`
- **ENTÃO** o sistema exibe ✅ na linha com número de rows e duração

#### Scenario: Resultado com erro
- **QUANDO** o servidor envia evento `result` com status `error`
- **ENTÃO** o sistema exibe ❌ na linha com mensagem de erro

#### Scenario: Retry automático
- **QUANDO** o servidor envia evento `retry`
- **ENTÃO** o sistema exibe 🔄 com contagem de tentativa (ex: "Retry 1/3")

---

### Requirement: Cancelamento de execução
O sistema DEVE permitir que o usuário cancele uma execução em andamento.

#### Scenario: Cancelamento pelo usuário
- **QUANDO** o usuário clica em "Cancel"
- **ENTÃO** o sistema aborta a conexão NDJSON
- **E** envia `POST /api/query/cancel/{sessionId}`
- **E** exibe estado de execução como cancelada

---

### Requirement: Tabela consolidada de resultados
O sistema DEVE exibir todos os resultados de todos os servidores em uma única tabela consolidada.

#### Scenario: Exibição pós-execução
- **QUANDO** o servidor envia evento `complete`
- **ENTÃO** o sistema exibe tabela com colunas: Server, Database, e colunas do resultado da query

#### Scenario: Filtro e paginação
- **QUANDO** o usuário digita no campo de filtro
- **ENTÃO** o sistema filtra as rows em tempo real
- **E** pagina resultados quando exceder 50 rows por página

---

### Requirement: Download de resultados
O sistema DEVE permitir download dos resultados em formato CSV.

#### Scenario: Download via API
- **QUANDO** o usuário clica em "Download CSV"
- **ENTÃO** o sistema faz `GET /api/query/download/{sessionId}?type=consolidated`
- **E** inicia download do arquivo CSV

#### Scenario: Abrir pasta de output
- **QUANDO** o usuário clica em "Open Folder"
- **ENTÃO** o sistema invoca Tauri IPC `show_in_folder` para abrir o explorador de arquivos

---

### Requirement: Feedback visual de resumo
O sistema DEVE exibir um resumo numérico da execução.

#### Scenario: Resumo ao completar
- **QUANDO** a execução termina (completa ou com erros)
- **ENTÃO** o sistema exibe: número de servidores, sucessos, falhas, total de rows, tempo total
