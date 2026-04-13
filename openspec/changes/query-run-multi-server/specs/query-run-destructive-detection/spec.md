## ADDED Requirements

### Requirement: Detecção de queries destrutivas
O sistema DEVE analisar a query SQL antes da execução e detectar comandos que modificam dados ou schema usando regex simples.

#### Cenário: Query DELETE detectada
- **QUANDO** query contém keyword DELETE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query UPDATE detectada
- **QUANDO** query contém keyword UPDATE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query INSERT detectada
- **QUANDO** query contém keyword INSERT como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query ALTER detectada
- **QUANDO** query contém keyword ALTER como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query DROP detectada
- **QUANDO** query contém keyword DROP como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query CREATE detectada
- **QUANDO** query contém keyword CREATE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query TRUNCATE detectada
- **QUANDO** query contém keyword TRUNCATE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query MERGE detectada
- **QUANDO** query contém keyword MERGE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query GRANT detectada
- **QUANDO** query contém keyword GRANT como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query REVOKE detectada
- **QUANDO** query contém keyword REVOKE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

#### Cenário: Query SET ROLE detectada
- **QUANDO** query contém keywords SET ROLE como primeiro comando significativo
- **ENTÃO** sistema identifica como query destrutiva e solicita confirmação

### Requirement: Ignorar comentários e CTEs
O sistema DEVE ignorar comentários SQL (`--`, `/* */`) e CTEs (`WITH ... AS`) ao identificar o primeiro keyword significativo.

#### Cenário: Query com comentário inicial
- **QUANDO** query começa com `-- comentário\nSELECT * FROM users`
- **ENTÃO** sistema ignora o comentário e identifica SELECT como seguro

#### Cenário: Query com CTE
- **QUANDO** query começa com `WITH temp AS (SELECT ...) SELECT * FROM temp`
- **ENTÃO** sistema identifica o primeiro keyword dentro da CTE (SELECT) para classificação

### Requirement: Confirmação interativa
O sistema DEVE exibir prompt de confirmação antes de executar queries destrutivas em múltiplos servidores.

#### Cenário: Usuário confirma execução
- **QUANDO** query destrutiva é detectada e usuário responde 'y' ou 'Y'
- **ENTÃO** sistema prossegue com a execução normal

#### Cenário: Usuário cancela execução
- **QUANDO** query destrutiva é detectada e usuário responde 'n' ou 'N' ou Ctrl+C
- **ENTÃO** sistema cancela a execução e encerra com exit code 130

#### Cenário: Informações exibidas na confirmação
- **QUANDO** query destrutiva é detectada
- **ENTÃO** sistema exibe: tipo de comando, número de servidores afetados, número de databases afetadas, preview da query
- **E** sistema pergunta: "Executar em todos os alvos? [y/N]"

### Requirement: Queries seguras não requerem confirmação
O sistema DEVE executar queries SELECT, SHOW, EXPLAIN, DESCRIBE sem solicitar confirmação.

#### Cenário: Query SELECT
- **QUANDO** query é apenas SELECT
- **ENTÃO** sistema executa sem confirmação

#### Cenário: Query EXPLAIN
- **QUANDO** query é EXPLAIN ou EXPLAIN ANALYZE
- **ENTÃO** sistema executa sem confirmação
