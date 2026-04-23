# Documentação de Especificação de Funcionalidades

## Visão Geral do Produto

FurLab é uma ferramenta CLI baseada em .NET projetada para automatizar tarefas comuns de desenvolvimento. Ela fornece uma interface unificada para operações de banco de dados, gerenciamento de arquivos, instalação de ferramentas de IA e gerenciamento de pacotes Windows.

## Lista de Funcionalidades

### Funcionalidades Principais

1. **Utilitários de Banco de Dados** (Backup, Restore, PgPass)
2. **Utilitário de Arquivos** (Combine)
3. **Integração com Claude Code**
4. **Integração com OpenCode**
5. **Gerenciador de Pacotes Winget**
6. **Query SQL & Exportação** (Multi-server & Multi-database)
7. **Utilitários Docker** (Postgres)
8. **Gerenciador de Features Windows**
9. **Gerenciamento de Configurações** (Servidores de Banco de Dados)

---

## Funcionalidade 1: Utilitários de Banco de Dados

### Objetivo

Fornecer utilitários para backup, restore e gerenciamento de credenciais de bancos de dados PostgreSQL.

### Descrição Detalhada

O comando `database` fornece funcionalidades para criar backups e restaurar bancos de dados PostgreSQL usando `pg_dump` e `pg_restore`, além de gerenciar o arquivo `pgpass.conf`.

### Sub-Funcionalidades

#### 1.1 Backup de Banco de Dados

Cria backups de bancos de dados PostgreSQL usando `pg_dump`.

#### 1.2 Restore de Banco de Dados

Restaura bancos de dados PostgreSQL usando `pg_restore` a partir de arquivos `.dump`.

#### 1.3 Gerenciamento de PgPass

Gerencia o arquivo `pgpass.conf` para permitir conexões sem senha. Suporta o uso de curingas (`*`) para host, porta e usuário.

### Fluxo de Uso

```bash
# Backup de um único banco de dados
fur database backup meubanco -H localhost -U postgres

# Backup de todos os bancos de dados
fur database backup --all -H localhost -U postgres -o "C:\backups"

# Restore de um banco de dados específico
fur database restore meubanco "C:\backups\meubanco.dump"

# Adicionar entrada ao pgpass
fur database pgpass add meubanco --host localhost --username postgres --password minhasenha

# Listar entradas do pgpass
fur database pgpass list
```

---

## Funcionalidade 2: Utilitários de Arquivo (Combine & Convert)

### Objetivo

Prover utilitários para manipulação de arquivos de texto, como combinação e conversão de codificação.

### Descrição Detalhada

O comando `file` fornece ferramentas para processamento de arquivos em lote, suportando padrões glob para seleção de arquivos.

### Sub-Funcionalidades

#### 2.1 Combinar Arquivos (Combine)

Pega múltiplos arquivos de entrada que correspondem a um padrão (glob) e os combina em um único arquivo de saída.

#### 2.2 Converter Codificação (ConvertEncoding)

Converte arquivos de texto entre diferentes encodings (ex: Latin1 para UTF-8). Possui detecção automática de encoding de origem, suporte a backup e preservação de metadados do arquivo.

### Fluxo de Uso

```bash
# Combinar todos os arquivos SQL em um diretório
fur file combine -i "C:\temp\*.sql" -o "C:\temp\resultado.sql"

# Converter todos os arquivos .cs para UTF-8 (detecção automática)
fur file convert-encoding -i "**/*.cs" --to UTF-8

# Converter arquivos com backup e filtro de texto
fur file convert-encoding -i "docs/*" --from Windows-1252 --to UTF-8 --backup --text-only
```

---

## Funcionalidade 3: Integração com Claude Code

### Objetivo

Instalar e configurar o CLI do Claude Code.

### Fluxo de Uso

```bash
# Instalar Claude Code
fur claude install

# Configurar MCP de banco de dados
fur claude settings mcp-database

# Configurar ambiente Windows
fur claude settings win-env
```

---

## Funcionalidade 4: Integração com OpenCode

### Objetivo

Configurar a ferramenta CLI OpenCode.

### Fluxo de Uso

```bash
# Configurar MCP de banco de dados no OpenCode
fur opencode settings mcp-database

# Definir modelo padrão do OpenCode
fur opencode settings default-model claude-3-5-sonnet-20241022 --global
```

---

## Funcionalidade 5: Gerenciador de Pacotes Winget

### Objetivo

Fazer backup e restaurar pacotes Windows instalados via winget.

### Fluxo de Uso

```bash
# Backup de pacotes
fur winget backup -o "C:\backups"

# Restaurar pacotes
fur winget restore -i "C:\backups\winget-import.json"
```

---

## Funcionalidade 6: Query SQL & Exportação

### Objetivo
Executa scripts SQL em um ou mais bancos de dados/servidores e exporta para CSV.

### Descrição Detalhada
Suporta execução em paralelo em múltiplos servidores configurados em `settings db-servers`.

### Fluxo de Uso
```bash
# Executar em um banco específico
fur query run -f script.sql -d mydb -H localhost

# Executar em TODOS os servidores configurados
fur query run -f script.sql --all
```

---

## Funcionalidade 7: Utilitários Docker

### Objetivo
Gerenciar containers Docker úteis para desenvolvimento.

### Sub-Funcionalidades
- **Postgres**: Inicia um container PostgreSQL local com configurações padrão.

### Fluxo de Uso
```bash
fur docker postgres
```

---

## Funcionalidade 8: Gerenciador de Funcionalidades do Windows

### Objetivo
Backup e restore de Features opcionais do Windows.

### Fluxo de Uso
```bash
fur windowsfeatures list
fur windowsfeatures export -o "C:\backups\features.json"
fur windowsfeatures import -i "C:\backups\features.json"
```

---

## Funcionalidade 9: Gerenciamento de Configurações

### Objetivo
Gerenciar servidores de banco de dados configurados para uso global no FurLab.

### Descrição Detalhada
Permite cadastrar servidores com credenciais criptografadas para facilitar o uso nos comandos `query` e `database`.

### Fluxo de Uso
```bash
# Listar servidores
fur settings db-servers ls

# Adicionar servidor
fur settings db-servers add PROD --host 10.0.0.1 --username admin --database principal

# Definir senha (criptografada)
fur settings db-servers set-password PROD

# Testar conexão
fur settings db-servers test PROD
```

---

## Apêndice: Referência de Comandos

| Comando | Descrição |
|---------|-----------|
| `fur database` | Utilitários PostgreSQL (backup, restore, pgpass) |
| `fur file` | Utilitários de arquivo (combine, convert-encoding) |
| `fur claude` | Instalação e configuração do Claude Code |
| `fur opencode` | Configuração do OpenCode |
| `fur winget` | Backup e restore de pacotes winget |
| `fur query run` | Execução de SQL multi-server com exportação CSV |
| `fur docker` | Utilitários Docker (postgres) |
| `fur windowsfeatures` | Gerenciar features do Windows (dism) |
| `fur settings db-servers` | Gerenciar servidores de banco de dados cadastrados |
