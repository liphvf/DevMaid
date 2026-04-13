## MODIFICADO Requisitos

### Requisito: Formato do CSV de output
O sistema DEVE gerar arquivos CSV em uma subpasta por execução, com CSVs parciais por servidor escritos progressivamente (append) e um CSV consolidado gerado ao final com header unificado. Erros são registrados em arquivo dedicado `_erros.csv`.

#### Cenário: CSV com execução bem-sucedida
- **QUANDO** query executa com sucesso em um servidor/database
- **ENTÃO** sistema faz append no CSV parcial do servidor `results/<timestamp>/<server>_<timestamp>.csv`
- **E** CSV parcial inclui colunas: Server, Database, <colunas de resultado da query>
- **E** cada linha de resultado da query é precedida por Server e Database
- **E** se for a primeira query naquele servidor, header é escrito; caso contrário, apenas dados são adicionados
- **E** header inconsistente (databases com colunas diferentes) é ACEITÁVEL

#### Cenário: Log de execução no terminal
- **QUANDO** query executa em um servidor/database (sucesso ou falha)
- **ENTÃO** sistema exibe no feed de atividades: status, rows/erro, duração, caminho do arquivo
- **E** sucesso é exibido em verde com checkmark
- **E** erro é exibido em vermelho com X

#### Cenário: Múltiplos servidores — CSV consolidado
- **QUANDO** query executa em múltiplos servidores/databases
- **ENTÃO** ao final, sistema gera `results/<timestamp>/consolidated_<timestamp>.csv` com merge dos parciais
- **E** CSV consolidado contém uma linha por resultado de query
- **E** cada linha identifica qual servidor e database produziu aquele resultado
- **E** colunas de resultado são a união de todas as colunas, na ordem de primeira aparição

#### Cenário: Erros em arquivo dedicado
- **QUANDO** uma query falha em servidor/database
- **ENTÃO** erros são registrados em `results/<timestamp>/<timestamp>_erros.csv`
- **E** arquivo tem colunas: Server, Database, ExecutedAt, Error
- **E** erros NÃO geram linhas nos CSVs de resultado (parciais ou consolidado)

#### Cenário: Organização em subpasta
- **QUANDO** execução inicia
- **ENTÃO** sistema cria subpasta `results/<timestamp>/`
- **E** todos os arquivos CSV (parciais, consolidado, erros) ficam nesta subpasta

## REMOVIDO Requisitos

### Requisito: CSV com --separate-files
**Razão**: O comportamento de `--separate-files` (um CSV por servidor) é agora o comportamento padrão — sempre gera CSV por servidor com append progressivo. A flag se torna redundante.
**Migração**: Usuários que usavam `--separate-files` terão saída idêntica (por servidor), mas agora progressiva e em subpasta.

### Requisito: Output path default
**Razão**: Substituído pelo requisito de organização em subpasta. O path base continua o mesmo (`outputDirectory` das defaults ou `-o`), mas os arquivos são organizados dentro de uma subpasta `<timestamp>`.
**Migração**: Path base inalterado. Subpasta é criada automaticamente dentro do path base.