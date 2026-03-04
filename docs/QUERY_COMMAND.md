# Query Command

O comando `query` permite executar queries SQL e exportar os resultados para CSV. Suporta execução em banco único ou múltiplos bancos simultaneamente.

## Uso

### Query em banco único

```bash
devmaid query run --input <arquivo.sql> --output <arquivo.csv> [opções]
```

### Query em múltiplos bancos

```bash
devmaid query run --all --input <arquivo.sql> --output <diretorio> [opções]
```

## Opções

### Obrigatórias

- `-i, --input <input>`: Caminho para o arquivo SQL de entrada
- `-o, --output <output>`:
  - Para banco único: Caminho para o arquivo CSV de saída
  - Para modo `--all`: Diretório onde os arquivos CSV serão salvos

### Opções Multi-Database

- `-a, --all`: Executa a query em todos os bancos de dados no servidor
- `--separate-files`: Gera arquivos CSV separados para cada banco (um arquivo por banco)
- `--exclude <databases>`: Lista de bancos para excluir, separados por vírgula (ex: `postgres,template0,template1`)

### Connection String

- `--npgsql-connection-string <string>`: Connection string completa do Npgsql (ex: `"Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass"`)
  - Se fornecida, tem precedência sobre os parâmetros individuais

### Parâmetros de Conexão Individuais

- `-h, --host <host>`: Endereço do host do banco de dados
- `-p, --port <port>`: Porta do banco de dados
- `-d, --database <database>`: Nome do banco de dados (não obrigatório quando usando `--all`)
- `-U, --username <username>`: Nome de usuário do banco de dados
- `-W, --password <password>`: Senha do banco de dados (se não fornecida, será solicitada interativamente)

### Opções de Conexão

- `--ssl-mode <mode>`: Modo SSL (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull). Padrão: Prefer
- `--timeout <seconds>`: Timeout de conexão em segundos. Padrão: 30
- `--command-timeout <seconds>`: Timeout do comando em segundos. Padrão: 300
- `--pooling`: Habilita connection pooling. Padrão: true
- `--min-pool-size <size>`: Tamanho mínimo do pool. Padrão: 1
- `--max-pool-size <size>`: Tamanho máximo do pool. Padrão: 100
- `--keepalive <seconds>`: Intervalo de keepalive em segundos. Padrão: 0
- `--connection-lifetime <seconds>`: Tempo de vida da conexão em segundos. Padrão: 0

## Exemplos

### Usando connection string completa

```bash
devmaid query run --input query.sql --output results.csv --npgsql-connection-string "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass"
```

### Usando parâmetros individuais

```bash
devmaid query run --input query.sql --output results.csv --host localhost --port 5432 --database mydb --username myuser
# Será solicitada a senha interativamente
```

### Com opções de SSL

```bash
devmaid query run --input query.sql --output results.csv --host localhost --port 5432 --database mydb --username myuser --password mypass --ssl-mode Require
```

### Com timeout personalizado

```bash
devmaid query run --input query.sql --output results.csv --host localhost --port 5432 --database mydb --username myuser --password mypass --command-timeout 600
```

## Query Multi-Database

A funcionalidade `--all` permite executar a mesma query em múltiplos bancos de dados simultaneamente. Isso é útil para:

- Auditoria de dados em múltiplos bancos
- Coletar métricas de todos os bancos de um servidor
- Comparar dados entre diferentes bancos
- Relatórios consolidados de múltiplas fontes

### Modos de Saída

#### Modo Consolidado (padrão)

Todos os resultados são combinados em um único arquivo `all_databases.csv` com uma coluna adicional `_database_name` indicando a origem de cada linha.

```bash
devmaid query run --all --input query.sql --output ./results
```

**Saída:**
- Arquivo: `results/all_databases.csv`
- Colunas: `_database_name`, [colunas da query]
- Cada linha inclui o nome do banco de origem

#### Modo Arquivos Separados

Gera um arquivo CSV separado para cada banco de dados.

```bash
devmaid query run --all --separate-files --input query.sql --output ./results
```

**Saída:**
- Arquivo por banco: `results/{database}.csv`
- Exemplo: `results/app_prod.csv`, `results/app_dev.csv`, `results/app_test.csv`

### Excluindo Bancos

Use `--exclude` para pular bancos específicos, como bancos do sistema ou bancos temporários.

```bash
devmaid query run --all --exclude postgres,template0,template1 --input query.sql --output ./results
```

### Exemplos Completos

#### Auditoria de tamanho de tabelas em todos os bancos

```bash
devmaid query run --all --exclude postgres,template0,template1 --input audit_tables.sql --output ./audit_results --separate-files
```

**Arquivo `audit_tables.sql`:**
```sql
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM
    pg_tables
WHERE
    schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY
    pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

#### Relatório consolidado de usuários

```bash
devmaid query run --all --exclude postgres,template0,template1 --input users_report.sql --output ./reports
```

**Arquivo `users_report.sql`:**
```sql
SELECT
    usename as username,
    usecreated as created_at,
    usesuper as is_superuser,
    useconfig as config
FROM
    pg_user
ORDER BY
    usename;
```

#### Contagem de registros por tabela em todos os bancos

```bash
devmaid query run --all --exclude postgres,template0,template1 --input count_tables.sql --output ./counts --separate-files
```

**Arquivo `count_tables.sql`:**
```sql
SELECT
    schemaname,
    tablename,
    n_tup_ins as inserted,
    n_tup_upd as updated,
    n_tup_del as deleted,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows
FROM
    pg_stat_user_tables
ORDER BY
    schemaname, tablename;
```

## Requisitos

### Para Query Multi-Database

- **psql** deve estar instalado e no PATH
- O usuário deve ter permissão de conexão com o banco `postgres`
- Permissão de leitura em `pg_database` para listar bancos

### Windows - Localização Automática do psql

A ferramenta procura automaticamente o `psql.exe` em:
- PATH do sistema
- `C:\Program Files\PostgreSQL\*\bin\psql.exe`
- `C:\PostgreSQL\*\bin\psql.exe`

Se o psql não for encontrado, ocorrerá um erro informando que o PostgreSQL precisa ser instalado.

## Configuração Padrão

Você pode configurar valores padrão no arquivo `appsettings.json`:

```json
{
  "Database": {
    "Host": "localhost",
    "Port": "5432",
    "Database": "mydb",
    "Username": "myuser",
    "Password": "mypassword"
  }
}
```

**Nota:** Ao usar `--all`, o parâmetro `Database` não é obrigatório, pois a query será executada em todos os bancos listados no servidor.

## Arquivo SQL de Exemplo

```sql
-- Consulta todas as tabelas do banco de dados
SELECT
    schemaname,
    tablename,
    tableowner
FROM
    pg_tables
WHERE
    schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY
    tablename;
```

## Saída CSV

### Modo Banco Único

O arquivo CSV de saída conterá:
- Cabeçalho com os nomes das colunas
- Linhas com os dados do resultado da query
- Valores nulos são representados como campos vazios
- Campos com vírgulas são automaticamente delimitados

### Modo Multi-Database

#### Consolidado (sem `--separate-files`)

Arquivo único `all_databases.csv`:
- Coluna adicional `_database_name` no início de cada linha
- Todas as colunas da query
- Linhas de todos os bancos combinadas em ordem de processamento

#### Arquivos Separados (com `--separate-files`)

Um arquivo CSV por banco:
- Nome do arquivo: `{database}.csv`
- Apenas as colunas da query (sem `_database_name`)
- Dados apenas do banco específico

### Progresso e Feedback

Durante a execução em múltiplos bancos:
- Lista de bancos a processar
- Progresso por banco (✓ sucesso, ✗ falha)
- Contagem de linhas processadas
- Resumo final com estatísticas:
  - Quantidade de bancos processados com sucesso
  - Quantidade de falhas
  - Total de linhas
  - Localização dos arquivos gerados

## Segurança

- Validação de paths para prevenir path traversal
- Validação de host, port e username
- Senha solicitada interativamente se não fornecida
- Validação de identificadores PostgreSQL