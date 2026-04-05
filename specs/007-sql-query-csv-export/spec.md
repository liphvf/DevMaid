# Spec de Feature: Consulta SQL e Exportação CSV

**ID:** 007  
**Slug:** sql-query-csv-export  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Permitir que desenvolvedores e engenheiros de dados executem consultas SQL contra bancos de dados PostgreSQL e exportem os resultados para CSV — suportando banco único, todos os bancos de um servidor e múltiplos servidores configurados no `appsettings.json` — para fluxos de trabalho de relatórios, auditoria e extração de dados.

---

## Histórias de Usuário

**HU-007.1** — Como desenvolvedor, quero executar um script SQL contra um banco específico e salvar os resultados como arquivo CSV, para poder compartilhar resultados de consultas com partes interessadas não técnicas.

**HU-007.2** — Como DBA, quero executar a mesma consulta em todos os bancos de dados de um servidor, para coletar métricas ou auditar dados em todo o servidor em uma única operação.

**HU-007.3** — Como DBA, quero executar uma consulta em múltiplos servidores configurados simultaneamente, para produzir relatórios entre ambientes sem repetição manual.

**HU-007.4** — Como desenvolvedor, quero filtrar quais servidores são incluídos usando um padrão curinga, para direcionar apenas servidores de produção ou apenas de staging.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-007.1 | `devmaid query run --input <sql> --output <csv>` executa o SQL e grava os resultados no arquivo CSV especificado com uma linha de cabeçalho. |
| CA-007.2 | `devmaid query run --all --input <sql> --output <dir>` cria `all_databases.csv` em `<dir>` com uma coluna prefixo `_database_name`. |
| CA-007.3 | `devmaid query run --all --separate-files --input <sql> --output <dir>` cria um arquivo `<banco>.csv` por banco em `<dir>`. |
| CA-007.4 | `devmaid query run --servers --input <sql> --output <dir>` executa em todos os servidores do `appsettings.json` com `Servers.Enabled = true`, criando `<dir>/<servidor>/<banco>.csv` para cada resultado. |
| CA-007.5 | `--server-filter <padrão>` limita a execução aos servidores cujo `Name` corresponde ao padrão curinga (sem distinção de maiúsculas/minúsculas, `*` suportado). |
| CA-007.6 | `--exclude <lista>` pula os nomes de bancos listados (separados por vírgula) ao usar `--all`. |
| CA-007.7 | Parâmetros de conexão da CLI sobrescrevem os do `appsettings.json`. |
| CA-007.8 | Valores NULL nos resultados da consulta aparecem como campos vazios no CSV. |
| CA-007.9 | Campos contendo vírgulas ou aspas são devidamente escapados conforme RFC 4180. |
| CA-007.10 | O progresso é impresso por banco e por servidor durante operações com múltiplos alvos. |
| CA-007.11 | Um resumo é impresso ao final de operações com múltiplos alvos: total de bancos, com sucesso, com falha, total de linhas. |

---

## Interface CLI

```bash
# Banco único
devmaid query run --input <sql> --output <arquivo> [opções de conexão]

# Todos os bancos de um servidor
devmaid query run --all --input <sql> --output <diretório> [opções]

# Todos os servidores configurados
devmaid query run --servers --input <sql> --output <diretório> [opções]
```

### Opções

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `--input` | `-i` | Sim | — | Caminho para o arquivo SQL |
| `--output` | `-o` | Sim | — | Arquivo CSV de saída (único) ou diretório (múltiplo) |
| `--all` | `-a` | Não | `false` | Executar em todos os bancos do servidor |
| `--separate-files` | — | Não | `false` | Um CSV por banco (requer `--all`) |
| `--exclude` | — | Não | — | Nomes de bancos a pular, separados por vírgula |
| `--servers` | `-s` | Não | `false` | Executar em todos os servidores configurados |
| `--server-filter` | — | Não | — | Filtro curinga para nomes de servidores |
| `--host` | `-h` | Não | da config | Host do banco |
| `--port` | `-p` | Não | `5432` | Porta do banco |
| `--database` | `-d` | Não (com `--all`) | da config | Banco de destino |
| `--username` | `-U` | Não | da config | Nome de usuário |
| `--password` | `-W` | Não | solicitar | Senha |
| `--ssl-mode` | — | Não | `Prefer` | Modo SSL |
| `--timeout` | — | Não | `30` | Timeout de conexão (segundos) |
| `--command-timeout` | — | Não | `300` | Timeout de execução da query (segundos) |
| `--npgsql-connection-string` | — | Não | — | String de conexão Npgsql completa (sobrescreve parâmetros individuais) |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Todas as consultas concluídas com sucesso |
| `1` | Uma ou mais consultas falharam (sucesso parcial ainda é saída `1`) |
| `2` | Opção obrigatória ausente |
| `3` | psql não encontrado (necessário para `--all`) |

---

## Configuração Multi-Servidor

Os servidores são definidos no `appsettings.json`:

```json
{
  "Servers": {
    "Enabled": true,
    "PrimaryServer": "dev-local",
    "ServersList": [
      {
        "Name": "dev-local",
        "Host": "localhost",
        "Port": "5432",
        "Username": "postgres",
        "Password": "",
        "Database": "mydb",
        "Databases": [],
        "SslMode": "Prefer",
        "Timeout": 30,
        "CommandTimeout": 300
      }
    ]
  }
}
```

### Ordem de Resolução de Bancos (por servidor)

1. Lista `Databases` do servidor (se não vazia)
2. Flag `--all` → listar todos os bancos via psql (aplicando `--exclude`)
3. Campo `Database` padrão do servidor

### Precedência de Parâmetros de Conexão (da maior para a menor)

1. Flags da CLI (`--host`, `--port`, etc.)
2. Configuração específica do servidor em `ServersList`
3. Configuração do `PrimaryServer`
4. Padrões

---

## Estrutura de Saída

### Banco Único

```
result.csv                    ← cabeçalho + linhas
```

### Multi-Banco (Consolidado)

```
results/
└── all_databases.csv         ← _database_name + todas as colunas
```

### Multi-Banco (Arquivos Separados)

```
results/
├── app_prod.csv
├── app_dev.csv
└── app_test.csv
```

### Multi-Servidor

```
results/
├── prod-primary/
│   ├── app_prod.csv
│   └── analytics.csv
└── staging/
    └── app_staging.csv
```

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| Arquivo SQL não encontrado | Sair `1`, mensagem: `"Arquivo de entrada '<caminho>' não encontrado."` |
| Erro de sintaxe SQL | Sair `1`, imprimir mensagem de erro do PostgreSQL |
| Falha de conexão | Registrar falha para aquele banco, continuar com os demais no modo múltiplo |
| psql não encontrado (para `--all`) | Sair `3`, mensagem com instruções de instalação do PostgreSQL |
| Nenhum servidor corresponde ao `--server-filter` | Sair `1`, mensagem: `"Nenhum servidor encontrado correspondendo ao padrão '<padrão>'."` |
| `Servers.Enabled = false` com `--servers` | Sair `1`, mensagem: `"Configuração multi-servidor não está habilitada no appsettings.json."` |
| `PrimaryServer` não configurado | Sair `1`, mensagem com instruções de correção |

---

## Requisitos Não Funcionais

- Deve processar conjuntos de resultados de até **1 milhão de linhas** sem manter todas as linhas na memória (gravação em streaming).
- A execução multi-servidor deve processar servidores **sequencialmente** (não em paralelo) para evitar sobrecarga na rede ou no gerenciamento de credenciais.
- A saída de progresso não deve interferir no arquivo CSV de saída.
