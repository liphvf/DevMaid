# Query Command

O comando `query` permite executar queries SQL em múltiplos servidores PostgreSQL e exportar os resultados para CSV.

## Uso

```bash
FurLab query run -i <arquivo.sql> [opções]
FurLab query run -c "SELECT * FROM users" [opções]
```

## Fluxo de Execução

1. **Query**: Forneça via `-i` (arquivo) ou `-c` (inline)
2. **Seleção de Servidores**: Prompt interativo mostra servidores configurados (todos pré-selecionados)
3. **Confirmação**: Se a query for destrutiva, exibe confirmação com detalhes
4. **Execução Paralela**: Queries executam em paralelo com tolerância a falhas
5. **Exportação CSV**: Resultados são exportados para CSV

## Opções

### Query

| Opção | Descrição |
|-------|-----------|
| `-i, --input <arquivo>` | Caminho para arquivo SQL (mutuamente exclusivo com `-c`) |
| `-c, --command <sql>` | Query SQL inline (mutuamente exclusivo com `-i`) |

### Output

| Opção | Descrição |
|-------|-----------|
| `-o, --output <caminho>` | Arquivo CSV ou diretório de saída |
| `--separate-files` | Gera um CSV por servidor ao invés de consolidado |

### Multi-Database

| Opção | Descrição |
|-------|-----------|
| `-a, --all` | Executa em todas as databases do servidor |
| `--exclude <dbs>` | Databases para excluir (separadas por vírgula) |

### Confirmação

| Opção | Descrição |
|-------|-----------|
| `--no-confirm` | Pula confirmação de queries destrutivas |

## Formato do CSV

### Consolidado (padrão)

Arquivo único com colunas: `Server, Database, <colunas da query>`

```
Server,Database,id,name
dev,mydb,1,Alice
dev,mydb,2,Bob
prod,mydb,1,Alice
```

### Arquivos Separados (`--separate-files`)

Um CSV por servidor: `<server>_<timestamp>.csv`

Cada arquivo mantém o mesmo formato com colunas `Server, Database, <colunas da query>`.

### Notas

- Falhas NÃO geram linhas no CSV — são logadas no terminal
- O terminal exibe uma tabela resumo com: Server, Database, Status, Rows, ExecutedAt, Error

## Detecção de Queries Destrutivas

Antes da execução, FurLab analisa a query para detectar comandos destrutivos:

**Keywords destrutivas**: INSERT, UPDATE, DELETE, ALTER, DROP, CREATE, TRUNCATE, MERGE, GRANT, REVOKE, SET ROLE

Quando detectada:
- Exibe tipo de query, servidores/databases afetados, preview
- Pede confirmação antes de prosseguir
- Use `--no-confirm` para pular em CI/scripts

Comentários SQL (`--`, `/* */`) e CTEs (`WITH ... AS`) são ignorados na análise.

## Configuração de Servidores

Servidores são configurados via `settings db-servers`:

```bash
# Adicionar servidor
FurLab settings db-servers add -n dev -h localhost -p 5432 -U postgres -W mypass

# Listar servidores
FurLab settings db-servers ls

# Testar conexão
FurLab settings db-servers test -n dev
```

Veja [MULTI_SERVER.md](MULTI_SERVER.md) para documentação completa.
