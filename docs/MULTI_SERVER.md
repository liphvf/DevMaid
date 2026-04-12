# Multi-Server Query Execution

O comando `query` suporta execução em múltiplos servidores PostgreSQL com seleção interativa, execução paralela e exportação CSV.

## Configuração

### Usando `settings db-servers`

Servidores são gerenciados via CLI (armazenados em `%LocalAppData%\FurLab\furlab.jsonc`):

```bash
# Adicionar servidor interativamente
FurLab settings db-servers add -i

# Adicionar servidor com flags
FurLab settings db-servers add -n dev -h localhost -p 5432 -U postgres -W mypass \
    -d mydb,app_dev --ssl Prefer --timeout 30 --command-timeout 300

# Adicionar com auto-descoberta de databases
FurLab settings db-servers add -n prod -h prod-db.com -U readonly -W secret \
    --fetch-all --exclude-patterns "template*,postgres"

# Listar servidores
FurLab settings db-servers ls

# Testar conexão
FurLab settings db-servers test -n dev

# Remover servidor
FurLab settings db-servers rm -n dev
```

### Estrutura do furlab.jsonc

O arquivo `furlab.jsonc` em `%LocalAppData%\FurLab\` suporta comentários:

```jsonc
{
  // Servidores PostgreSQL
  "servers": [
    {
      "name": "dev",               // Identificador único (obrigatório)
      "host": "localhost",          // Host (obrigatório)
      "port": 5432,                 // Porta (default: 5432)
      "username": "postgres",       // Usuário (obrigatório)
      "password": "mypassword",     // Senha (opcional)
      "databases": ["mydb"],        // Databases específicas
      "fetchAllDatabases": false,   // Auto-descoberta (default: false)
      "excludePatterns": ["template*", "postgres"],  // Patterns de exclusão
      "sslMode": "Prefer",          // SSL (default: Prefer)
      "timeout": 30,                // Timeout de conexão (default: 30)
      "commandTimeout": 300,        // Timeout do comando (default: 300)
      "maxParallelism": 4           // Paralelismo por servidor (default: 4)
    }
  ],
  // Defaults
  "defaults": {
    "outputFormat": "csv",
    "outputDirectory": "./results",
    "fetchAllDatabases": false,
    "requireConfirmation": true,
    "maxParallelism": 4
  }
}
```

## Fluxo de Execução

### 1. Seleção Interativa de Servidores

Ao executar `query run`, todos os servidores configurados são exibidos num prompt de seleção múltipla (todos pré-selecionados). Você pode desmarcar servidores que não deseja usar.

### 2. Detecção de Queries Destrutivas

Se a query contém keywords destrutivas (INSERT, UPDATE, DELETE, ALTER, DROP, CREATE, TRUNCATE, MERGE, GRANT, REVOKE, SET ROLE), uma confirmação é exibida antes de prosseguir.

### 3. Execução Paralela

Queries executam em paralelo em todos os servidores/databases selecionados, com:
- `MaxDegreeOfParallelism` configurável por servidor
- Polly retries automáticos (3 tentativas, backoff exponencial) para falhas transitórias
- Tolerância a falhas: se um servidor falha, os outros continuam

### 4. Log e Exportação

- **Terminal**: Log por database (`✓ dev/db1 — Success — 5 rows (14:30:22)`)
- **Terminal**: Tabela resumo final (Server, Database, Status, Rows, ExecutedAt, Error)
- **CSV**: Apenas resultados de sucesso (colunas: `Server, Database, <query cols>`)

## Exemplos

### Query em servidores selecionados

```bash
FurLab query run -i audit.sql -o ./results
```

### Query inline

```bash
FurLab query run -c "SELECT count(*) FROM users" -o ./results
```

### CSV por servidor

```bash
FurLab query run -i query.sql --separate-files -o ./results
```

Gera: `dev_20260412_143022.csv`, `prod_20260412_143022.csv`

### Auto-descoberta de databases

```bash
# Servidor com fetchAllDatabases: true no furlab.jsonc
FurLab query run -i query.sql -o ./results
```

## Saída CSV

### Consolidado (padrão)

```
Server,Database,id,name
dev,mydb,1,Alice
dev,mydb,2,Bob
prod,mydb,1,Alice
```

### Separado (`--separate-files`)

Um arquivo por servidor: `<server>_<timestamp>.csv`

### Comportamento com Erros

- Erros são logados no terminal (não no CSV)
- Se todos os servidores falham, exibe: "Nenhum servidor respondeu com sucesso. Verifique as conexões." (exit code 1)

## Migração do appsettings.json

Se você usava `appsettings.json` para configurar servidores:

1. Use `settings db-servers add` para recriar servidores no novo formato
2. O `appsettings.json` ainda funciona como fallback durante transição
3. Quando ambos existem, `furlab.jsonc` tem precedência
