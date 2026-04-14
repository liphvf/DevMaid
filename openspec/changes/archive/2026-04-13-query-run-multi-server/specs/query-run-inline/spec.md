## ADDED Requirements

### Requirement: Parâmetro -c para query inline
O sistema DEVE aceitar o parâmetro `-c` para passar uma query SQL diretamente na linha de comando, similar ao comportamento do `psql -c`.

#### Cenário: Query inline válida
- **QUANDO** usuário executa `query run -c "SELECT count(*) FROM users"`
- **ENTÃO** sistema executa a query informada nos servidores selecionados

#### Cenário: Query inline com aspas
- **QUANDO** usuário executa `query run -c "SELECT * FROM users WHERE name = 'John'"`
- **ENTÃO** sistema executa a query corretamente com aspas internas preservadas

#### Cenário: Query inline vazia
- **QUANDO** usuário executa `query run -c ""`
- **ENTÃO** sistema exibe mensagem de erro: "Query não pode ser vazia."
- **E** sistema encerra com exit code 2

### Requirement: Mutual exclusão entre -c e -i
O sistema DEVE tratar `-c` e `-i` como mutuamente exclusivos. Apenas um pode ser fornecido por vez.

#### Cenário: Ambos -c e -i fornecidos
- **QUANDO** usuário executa `query run -c "SELECT 1" -i arquivo.sql`
- **ENTÃO** sistema exibe mensagem de erro: "As opções -c e -i são mutuamente exclusivas. Use apenas uma."
- **E** sistema encerra com exit code 2

#### Cenário: Nenhum -c ou -i fornecido
- **QUANDO** usuário executa `query run` sem `-c` nem `-i`
- **ENTÃO** sistema exibe mensagem de erro: "É necessário fornecer uma query via -c ou um arquivo via -i."
- **E** sistema encerra com exit code 2

### Requirement: Validação de arquivo SQL
O sistema DEVE validar o arquivo SQL fornecido via `-i` antes da execução.

#### Cenário: Arquivo não existe
- **QUANDO** usuário executa `query run -i arquivo_inexistente.sql`
- **ENTÃO** sistema exibe mensagem de erro: "Arquivo não encontrado: arquivo_inexistente.sql"
- **E** sistema encerra com exit code 2

#### Cenário: Arquivo vazio
- **QUANDO** usuário executa `query run -i arquivo_vazio.sql` e o arquivo está vazio
- **ENTÃO** sistema exibe mensagem de erro: "Arquivo SQL está vazio."
- **E** sistema encerra com exit code 2

#### Cenário: Path traversal detectado
- **QUANDO** usuário fornece caminho com path traversal (ex: `../../etc/passwd`)
- **ENTÃO** sistema rejeita o caminho e exibe mensagem de erro
- **E** sistema encerra com exit code 2
