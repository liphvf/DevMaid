# Multi-Server Query Execution

O comando `query` suporta execução em múltiplos servidores PostgreSQL configurados no `appsettings.json`, gerando arquivos CSV organizados por servidor e banco de dados.

## Configuração

### 1. Configurar Servidores no appsettings.json

Edite o arquivo `appsettings.json` e configure a seção `Servers`:

```json
{
  "Servers": {
    "Enabled": true,
    "PrimaryServer": "localhost",
    "ServersList": [
      {
        "Name": "localhost",
        "Host": "localhost",
        "Port": "5432",
        "Username": "postgres",
        "Password": "",
        "Database": "mydb",
        "Databases": []
      }
    ]
  }
}
```

### Estrutura de Configuração

- **Enabled**: Ativa/desativa o modo multi-server (para `--servers`)
- **PrimaryServer**: Nome do servidor a ser usado por padrão em comandos sem `--servers`
- **ServersList**: Lista de servidores configurados

### 2. Configurar Servidores

Para cada servidor, você pode especificar:

| Campo | Obrigatório | Descrição |
|-------|-------------|-----------|
| `Name` | ✅ | Identificador único do servidor (usado para nomear diretórios e referenciar o PrimaryServer) |
| `Host` | ✅ | Endereço do host do PostgreSQL |
| `Port` | ❌ | Porta (padrão: 5432) |
| `Username` | ✅ | Nome de usuário |
| `Password` | ✅ | Senha |
| `Database` | ❌ | Banco padrão para esse servidor (usado quando não especificado --all e Databases está vazio) |
| `Databases` | ❌ | Lista de bancos para consultar (vazio = todos ou padrão do servidor) |
| `SslMode` | ❌ | Modo SSL (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull) |
| `Timeout` | ❌ | Timeout de conexão em segundos |
| `CommandTimeout` | ❌ | Timeout do comando em segundos |
| `Pooling` | ❌ | Habilitar connection pooling |
| `MinPoolSize` | ❌ | Tamanho mínimo do pool |
| `MaxPoolSize` | ❌ | Tamanho máximo do pool |
| `Keepalive` | ❌ | Intervalo de keepalive em segundos |
| `ConnectionLifetime` | ❌ | Tempo de vida da conexão em segundos |

## Uso

### Executar em Todos os Servidores Configurados

```bash
devmaid query run --servers --input query.sql --output ./results
```

**Saída:**
```
results/
├── prod-primary/
│   ├── app_prod.csv
│   └── analytics.csv
├── prod-secondary/
│   ├── app_prod.csv
│   └── analytics.csv
└── staging/
    └── app_staging.csv
```

### Filtrar Servidores por Nome

Use `--server-filter` para selecionar servidores específicos:

```bash
# Apenas servidores de produção
devmaid query run --servers --server-filter "prod-*" --input query.sql --output ./results

# Apenas servidores primários
devmaid query run --servers --server-filter "*-primary" --input query.sql --output ./results

# Apenas servidor específico
devmaid query run --servers --server-filter "staging" --input query.sql --output ./results
```

O filtro suporta o caractere curinga `*` e é case-insensitive.

### Combinar com --all (Todos os Bancos)

Se um servidor não tiver a lista `Databases` configurada, você pode usar `--all` para consultar todos os bancos:

```bash
devmaid query run --servers --all --input query.sql --output ./results
```

### Excluir Bancos Específicos

Use `--exclude` para pular bancos do sistema:

```bash
devmaid query run --servers --all --exclude "postgres,template0,template1" --input query.sql --output ./results
```

## Comportamento

### Determinação de Bancos a Consultar

Para cada servidor, a ferramenta segue esta ordem de prioridade:

1. **Lista `Databases` configurada**: Usa os bancos especificados na configuração do servidor
2. **Flag `--all`**: Lista todos os bancos no servidor (aplicando `--exclude` se fornecido)
3. **Banco padrão do servidor**: Usa o banco configurado em `Database` na configuração do servidor

### Prioridade de Configuração

As configurações de conexão seguem esta ordem de precedência (do maior para o menor):

1. **Opções da linha de comando** (`--host`, `--port`, `--database`, `--ssl-mode`, `--timeout`, etc.)
2. **Configuração específica do servidor** (no `Servers:ServersList`)
3. **Configuração do PrimaryServer** (servidor referenciado em `Servers:PrimaryServer`)

### Servidor Primário

Quando você executa um comando sem `--servers`, a ferramenta usa o servidor configurado em `Servers:PrimaryServer`:

```bash
# Usa o servidor configurado em PrimaryServer
devmaid query run --input query.sql --output result.csv

# Sobrescreve configurações do PrimaryServer
devmaid query run --input query.sql --output result.csv --host other-host.com --database otherdb
```

## Exemplos

### Usando o Servidor Primário

```bash
# Usa o servidor configurado em PrimaryServer
devmaid query run --input query.sql --output result.csv
```

### Auditoria em Todos os Servidores de Produção

```bash
devmaid query run --servers --server-filter "prod-*" \
    --input audit_tables.sql \
    --output ./audit_results \
    --exclude "postgres,template0,template1"
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

### Relatório de Usuários com Timeout Estendido

```bash
devmaid query run --servers --input users_report.sql \
    --output ./reports \
    --command-timeout 600
```

### Consulta Apenas em Servidores com SSL

Os servidores com `SslMode` configurado usarão suas configurações específicas:

```json
{
  "Servers": {
    "ServersList": [
      {
        "Name": "prod-secure",
        "Host": "prod-db.company.com",
        "Port": "5432",
        "Username": "readonly",
        "Password": "securepass",
        "Database": "app_prod",
        "SslMode": "VerifyFull"
      }
    ]
  }
}
```

## Saída e Feedback

### Progresso Durante Execução

```
Found 3 servers to process:
  - prod-primary (prod-db-01.company.com:5432)
  - prod-secondary (prod-db-02.company.com:5432)
  - staging (staging-db.company.com:5432)

Output directory: C:\Users\user\results

========================================
Processing server: prod-primary
Host: prod-db-01.company.com:5432
========================================

Using configured databases: app_prod, analytics

  Processing database 'app_prod'...
    ✓ Results exported to: app_prod.csv (1500 rows)
  Processing database 'analytics'...
    ✓ Results exported to: analytics.csv (250 rows)

Server 'prod-primary' summary:
  Successful: 2
  Failed: 0
  Total rows: 1750

========================================
Overall Execution Summary:
  Servers processed: 3
  Successful databases: 8
  Failed databases: 0
  Total rows: 5230
  Output directory: C:\Users\user\results
========================================
```

## Segurança

### Senhas

- As senhas são armazenadas em texto plano no `appsettings.json`
- **Recomendação**: Use variáveis de ambiente ou um sistema de gerenciamento de segredos em produção
- **Windows**: Use segredos do usuário ou Azure Key Vault
- **Linux**: Use variáveis de ambiente ou HashiCorp Vault

### Validação

- Validação de host, port e username
- Validação de paths para prevenir path traversal
- Validação de identificadores PostgreSQL

## Solução de Problemas

### Erro: "PrimaryServer is not configured in appsettings.json"

**Causa:** A propriedade `Servers:PrimaryServer` está ausente ou vazia.

**Solução:**
```json
{
  "Servers": {
    "PrimaryServer": "localhost",
    "ServersList": [...]
  }
}
```

### Erro: "Primary server 'X' not found in ServersList"

**Causa:** O servidor especificado em `PrimaryServer` não existe na lista `ServersList`.

**Solução:** Verifique se o nome do servidor em `PrimaryServer` corresponde exatamente ao `Name` de um servidor em `ServersList`.

### Erro: "Multi-server configuration is not enabled"

**Causa:** A configuração `Servers:Enabled` está definida como `false` ou ausente e você está usando `--servers`.

**Solução:**
```json
{
  "Servers": {
    "Enabled": true,
    "ServersList": [...]
  }
}
```

### Erro: "No servers configured in appsettings.json"

**Causa:** A lista `Servers:ServersList` está vazia ou ausente.

**Solução:** Adicione servidores à configuração.

### Erro: "No servers found matching filter pattern"

**Causa:** O filtro `--server-filter` não corresponde a nenhum servidor configurado.

**Solução:** Verifique o padrão do filtro e os nomes dos servidores configurados.

### Erro: "psql not found"

**Causa:** O `psql` não está instalado ou não está no PATH (necessário para `--all`).

**Solução:** Instale PostgreSQL e adicione `psql` ao PATH. No Windows, a ferramenta procura automaticamente em:
- `C:\Program Files\PostgreSQL\*\bin\psql.exe`
- `C:\PostgreSQL\*\bin\psql.exe`

## Melhores Práticas

### 1. Organização de Servidores

Use nomes descritivos que incluam ambiente e propósito:

```json
{
  "Servers": {
    "PrimaryServer": "dev-local",
    "ServersList": [
      { "Name": "prod-primary", ... },
      { "Name": "prod-secondary", ... },
      { "Name": "staging-primary", ... },
      { "Name": "dev-local", ... }
    ]
  }
}
```

### 2. Configuração de Bancos

Especifique bancos explicitamente quando possível:

```json
{
  "Name": "prod-app",
  "Database": "app_prod",
  "Databases": ["app_prod", "app_logs"]
}
```

Use `--all` apenas quando realmente precisar consultar todos os bancos.

### 3. Servidor Primário

Configure o `PrimaryServer` para desenvolvimento local:

```json
{
  "Servers": {
    "PrimaryServer": "dev-local",
    "ServersList": [
      {
        "Name": "dev-local",
        "Host": "localhost",
        "Port": "5432",
        "Username": "postgres",
        "Password": "",
        "Database": "mydb"
      }
    ]
  }
}
```

### 4. Timeouts

Ajuste timeouts para consultas pesadas:

```json
{
  "Name": "prod-analytics",
  "Database": "analytics",
  "CommandTimeout": 600
}
```

### 5. SSL em Produção

Sempre use SSL em servidores de produção:

```json
{
  "Name": "prod-secure",
  "Database": "app_prod",
  "SslMode": "VerifyFull"
}
```

### 6. Separar Configuração

Use arquivos de configuração diferentes por ambiente:

- `appsettings.json` - Desenvolvimento local (PrimaryServer: dev-local)
- `appsettings.staging.json` - Staging (PrimaryServer: staging-primary)
- `appsettings.production.json` - Produção (PrimaryServer: prod-primary, não commitar no Git)