## MODIFIED Requirements

### Requirement: Formato do CSV de output
O sistema DEVE gerar CSV consolidado com colunas de metadados adicionais para rastreamento de execução em múltiplos servidores.

#### Cenário: CSV com execução bem-sucedida
- **QUANDO** query executa com sucesso em um servidor/database
- **ENTÃO** CSV inclui linha com colunas: Server, Database, ExecutedAt, Status, RowCount, Error, <colunas de resultado da query>
- **E** Status = "Success"
- **E** RowCount = número de rows retornadas
- **E** Error = vazio
- **E** ExecutedAt = timestamp ISO 8601 da execução

#### Cenário: CSV com execução falha
- **QUANDO** query falha em um servidor/database
- **ENTÃO** CSV inclui linha com colunas: Server, Database, ExecutedAt, Status, RowCount, Error
- **E** Status = "Error"
- **E** RowCount = vazio
- **E** Error = mensagem de erro
- **E** ExecutedAt = timestamp ISO 8601 da tentativa

#### Cenário: Múltiplos servidores no mesmo CSV
- **QUANDO** query executa em múltiplos servidores/databases
- **ENTÃO** CSV contém uma linha por database executada
- **E** cada linha identifica qual servidor e database produziu aquele resultado
- **E** colunas de resultado da query são as mesmas para todas as linhas

#### Cenário: CSV com --separate-files
- **QUANDO** usuário executa com flag `--separate-files`
- **ENTÃO** sistema gera um arquivo CSV por database
- **E** cada arquivo tem nome: `<server>_<database>_<timestamp>.csv`
- **E** cada arquivo mantém o mesmo formato de colunas

### Requirement: Output path default
O sistema DEVE usar `outputDirectory` das configurações defaults como path base para arquivos CSV quando `-o` não é especificado.

#### Cenário: Output path não especificado
- **QUANDO** usuário não fornece `-o`
- **ENTÃO** sistema usa `%LocalAppData%\FurLab\results\query_<timestamp>.csv`

#### Cenário: Output path especificado
- **QUANDO** usuário fornece `-o caminho/arquivo.csv`
- **ENTÃO** sistema salva CSV naquele caminho
- **E** cria diretórios pai se não existirem
