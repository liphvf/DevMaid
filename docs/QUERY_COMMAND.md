# Query Command

O comando `query run` permite executar queries SQL em um ou mais servidores PostgreSQL e exportar os resultados para CSV. Ele suporta execução paralela, tratamento de falhas transientes (retry) e detecção de queries destrutivas.

## Uso

```bash
# Executar script em arquivo
fur query run -i <arquivo.sql>

# Executar query inline
fur query run -c "SELECT * FROM users"

# Executar em todos os servidores configurados ignorando confirmação
fur query run -i script.sql --all --no-confirm
```

## Fluxo de Execução

1. **Seleção de Query**: Forneça via `-i` (arquivo) ou `-c` (inline).
2. **Seleção de Servidores**: 
   - Se `--npgsql-connection-string` for fornecido, conecta-se diretamente.
   - Caso contrário, mostra um menu interativo com os servidores configurados em `settings db-servers`.
3. **Descoberta de Databases**: Se o servidor estiver configurado com `FetchAllDatabases` ou se `-a/--all` for usado, o FurLab descobre todas as databases acessíveis no servidor.
4. **Análise de Risco**: Detecta se a query contém comandos destrutivos (INSERT, UPDATE, DELETE, etc.).
5. **Confirmação**: Se a query for destrutiva, exibe um resumo dos alvos e pede confirmação (pode ser pulado com `--no-confirm`).
6. **Execução Paralela**: As queries são executadas em paralelo respeitando o limite de concorrência configurado. Usa **Polly** para tentar novamente em caso de falhas de rede.
7. **Saída Progressiva**: Os resultados são gravados em arquivos CSV parciais por servidor enquanto a query executa e consolidados ao final.

## Opções

### Entrada (Query)

| Opção | Atalho | Descrição |
|-------|--------|-----------|
| `--input` | `-i` | Caminho para arquivo SQL (mutuamente exclusivo com `-c`). |
| `--command` | `-c` | Query SQL inline (mutuamente exclusivo com `-i`). |

### Conexão (Overrides)

Estas opções sobrescrevem as configurações dos servidores selecionados ou definem uma conexão ad-hoc.

| Opção | Atalho | Descrição |
|-------|--------|-----------|
| `--npgsql-connection-string` | - | String de conexão completa. Pula seleção de servidor. |
| `--host` | `-H` | Host do banco de dados. |
| `--port` | `-p` | Porta do banco de dados. |
| `--database` | `-d` | Nome do banco de dados. |
| `--username` | `-U` | Usuário do banco de dados. |
| `--password` | `-W` | Senha (solicitada interativamente se não fornecida). |
| `--ssl-mode` | - | Modo SSL (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull). |
| `--timeout` | - | Timeout de conexão em segundos (padrão: 30). |
| `--command-timeout` | - | Timeout da query em segundos (padrão: 300). |

### Output

| Opção | Atalho | Descrição |
|-------|--------|-----------|
| `--output` | `-o` | Diretório de saída para os CSVs (padrão: configurado em defaults). |

### Filtros e Comportamento

| Opção | Atalho | Descrição |
|-------|--------|-----------|
| `--all` | `-a` | Executa em todas as databases descobertas em cada servidor. |
| `--exclude` | - | Lista de databases para excluir (ex: `temp_*, archive`). Aceita curingas. |
| `--no-confirm` | - | Pula confirmação para queries destrutivas. |

## Resultados e Logs

Os resultados são salvos em um diretório com timestamp dentro do caminho de output:

- **Consolidado**: `consolidated_<timestamp>.csv` — Contém Server, Database e as colunas da query.
- **Por Servidor**: `<server>_<timestamp>.csv` — Resultados específicos de cada servidor.
- **Erros**: `<timestamp>_erros.csv` — Detalhes de falhas (Server, Database, Erro).
- **Log**: `<timestamp>_log.csv` — Histórico completo da execução com métricas de tempo e linhas.

## Detecção de Queries Destrutivas

Keywords que disparam o alerta:
`INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`, `CREATE`, `TRUNCATE`, `MERGE`, `GRANT`, `REVOKE`, `SET ROLE`.

## Configuração de Servidores

Use o comando `settings db-servers` para gerenciar seus servidores. As senhas são armazenadas de forma criptografada e segura.
