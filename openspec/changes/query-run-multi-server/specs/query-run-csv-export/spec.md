## MODIFIED Requirements

### Requirement: Formato do CSV de output
O sistema DEVE gerar CSV com colunas de identificação (Server, Database) e colunas de resultado da query. Metadados de execução (ExecutedAt, Status, RowCount, Error) são logados apenas no terminal.

#### Cenário: CSV com execução bem-sucedida
- **QUANDO** query executa com sucesso em um servidor/database
- **ENTÃO** CSV inclui colunas: Server, Database, <colunas de resultado da query>
- **E** cada linha de resultado da query é precedida por Server e Database
- **E** falhas NÃO geram linhas no CSV (apenas log no terminal)

#### Cenário: Log de execução no terminal
- **QUANDO** query executa em um servidor/database (sucesso ou falha)
- **ENTÃO** sistema loga no terminal: `<server>/<database> — Success/Error — <rows ou erro> (<timestamp>)`
- **E** sucesso é exibido em verde
- **E** erro é exibido em vermelho

#### Cenário: Múltiplos servidores no mesmo CSV
- **QUANDO** query executa em múltiplos servidores/databases
- **ENTÃO** CSV contém uma linha por resultado de query
- **E** cada linha identifica qual servidor e database produziu aquele resultado
- **E** colunas de resultado da query são as mesmas para todas as linhas
- **E** tabela resumo no terminal mostra: Server, Database, Status, Rows, ExecutedAt, Error

#### Cenário: CSV com --separate-files
- **QUANDO** usuário executa com flag `--separate-files`
- **ENTÃO** sistema gera um arquivo CSV por servidor
- **E** cada arquivo tem nome: `<server>_<timestamp>.csv`
- **E** cada arquivo inclui colunas Server, Database, <colunas da query>

### Requirement: Output path default
O sistema DEVE usar `outputDirectory` das configurações defaults como path base para arquivos CSV quando `-o` não é especificado.

#### Cenário: Output path não especificado
- **QUANDO** usuário não fornece `-o`
- **ENTÃO** sistema usa `%LocalAppData%\FurLab\results\query_<timestamp>.csv`

#### Cenário: Output path especificado
- **QUANDO** usuário fornece `-o caminho/arquivo.csv`
- **ENTÃO** sistema salva CSV naquele caminho
- **E** cria diretórios pai se não existirem