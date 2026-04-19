# Multi-Server Query Execution

O comando `query run` suporta execução em múltiplos servidores PostgreSQL com seleção interativa, execução paralela e exportação CSV consolidada.

## Configuração

### Gerenciamento via CLI

Servidores são gerenciados via comando `settings db-servers`. As configurações são armazenadas em `%LocalAppData%\FurLab\furlab.jsonc`.

**Importante**: Por segurança, as senhas não são armazenadas em texto plano no arquivo JSON. Elas são criptografadas e gerenciadas pelo `ICredentialService`.

```bash
# Adicionar servidor interativamente
fur settings db-servers add -i

# Adicionar servidor com flags
fur settings db-servers add dev --host localhost --port 5432 --username postgres --database mydb

# Adicionar com auto-descoberta de databases
fur settings db-servers prod --host prod-db.com --username readonly --fetch-all --exclude-patterns "template*,postgres"

# Definir/Atualizar senha (armazenamento seguro criptografado)
fur settings db-servers set-password dev

# Listar servidores
fur settings db-servers ls

# Testar conexão
fur settings db-servers test dev

# Remover servidor
fur settings db-servers rm dev
```

### Estrutura do furlab.jsonc

O arquivo `furlab.jsonc` suporta comentários e segue esta estrutura:

```jsonc
{
  "servers": [
    {
      "name": "dev",
      "host": "localhost",
      "port": 5432,
      "username": "postgres",
      "encryptedPassword": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA...", // Senha criptografada
      "databases": ["mydb"],
      "fetchAllDatabases": false,
      "excludePatterns": ["template*", "postgres"],
      "sslMode": "Prefer",
      "timeout": 30,
      "maxParallelism": 4
    }
  ],
  "defaults": {
    "outputDirectory": "./results",
    "requireConfirmation": true,
    "maxParallelism": 4
  }
}
```

## Fluxo de Execução

### 1. Seleção Interativa de Servidores

Ao executar `query run`, se você não fornecer uma string de conexão direta, o FurLab exibe um menu de seleção múltipla com todos os servidores cadastrados. Por padrão, todos vêm pré-selecionados.

### 2. Detecção de Queries Destrutivas

O FurLab analisa o SQL em busca de comandos que alteram dados ou estrutura. Se detectados, um alerta vermelho é exibido com um resumo do impacto e uma solicitação de confirmação.

### 3. Execução Paralela e Resiliência

As queries rodam em paralelo:
- **Entre Servidores**: Múltiplos servidores são processados simultaneamente.
- **Entre Databases**: Se um servidor tem múltiplas databases, elas também podem ser processadas em paralelo (respeitando o `maxParallelism` do servidor).
- **Retry**: Falhas de conexão transientes são tratadas automaticamente com 3 tentativas e backoff exponencial.

### 4. Resultados Progressivos

Diferente de ferramentas tradicionais que esperam tudo terminar, o FurLab:
1. Cria uma pasta com timestamp para a execução.
2. Grava arquivos CSV parciais para cada servidor assim que os dados chegam.
3. Exibe uma tabela live no terminal com o status de cada database.
4. Gera um arquivo `consolidated_<timestamp>.csv` ao final com todos os resultados de sucesso.

## Exemplos de Uso

### Execução Simples

```bash
fur query run -i audit.sql
```

### Forçar execução em todas as databases (Auto-discovery)

```bash
fur query run -c "SELECT version()" --all
```

### Pular confirmação (Útil para automação)

```bash
fur query run -i script.sql --no-confirm
```

## Saída CSV

O arquivo consolidado possui as colunas originais da sua query acrescidas de:
- `Server`: Nome do servidor configurado.
- `Database`: Nome da database onde a linha foi lida.

### Tratamento de Erros

Se uma database falhar, o erro é registrado no arquivo `_erros.csv` daquela execução, mas o processo continua para as demais databases/servidores.
