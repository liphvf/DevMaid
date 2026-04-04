# Spec de Feature: Utilitários de Banco de Dados

**ID:** 002  
**Slug:** database-utilities  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Fornecer aos desenvolvedores uma maneira simples e unificada de fazer backup e restaurar bancos de dados PostgreSQL usando a cadeia de ferramentas padrão `pg_dump` / `pg_restore`, suportando operações em banco único e em massa.

---

## Histórias de Usuário

**HU-002.1** — Como desenvolvedor, quero criar um backup binário de um único banco de dados PostgreSQL, para poder restaurá-lo depois ou movê-lo para outro ambiente.

**HU-002.2** — Como desenvolvedor, quero fazer backup de todos os bancos de dados de um servidor com um único comando, para criar snapshots completos do ambiente de forma eficiente.

**HU-002.3** — Como desenvolvedor, quero restaurar um banco de dados a partir de um arquivo `.dump`, para recuperar dados ou replicar ambientes.

**HU-002.4** — Como desenvolvedor, quero restaurar todos os arquivos `.dump` de um diretório, para reconstruir um ambiente completo a partir de um conjunto de backups.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-002.1 | Executar `devmaid database backup <banco>` cria `<banco>.dump` no diretório atual usando `pg_dump --format=custom`. |
| CA-002.2 | Executar `devmaid database backup --all` cria um arquivo `.dump` por banco encontrado no servidor (excluindo `template0`, `template1`, `postgres` a menos que explicitamente incluídos). |
| CA-002.3 | Quando `--output` é especificado para backup, os arquivos dump são criados nesse diretório. |
| CA-002.4 | Executar `devmaid database restore <banco> <arquivo>` executa `pg_restore` contra o banco e arquivo especificados. |
| CA-002.5 | Se o banco de destino não existir durante a restauração, ele é criado automaticamente antes de invocar o `pg_restore`. |
| CA-002.6 | Executar `devmaid database restore --all` restaura cada arquivo `.dump` encontrado no diretório atual (ou `--directory` se especificado). |
| CA-002.7 | O nome do banco é inferido a partir do nome base do arquivo dump (sem extensão) ao usar `--all`. |
| CA-002.8 | Senhas são solicitadas interativamente se não fornecidas via `--password`. |

---

## Interface CLI

### Backup

```bash
devmaid database backup [<banco>] [opções]
devmaid database backup --all [opções]
```

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `<banco>` | — | Sim (sem `--all`) | — | Banco a fazer backup |
| `--all` | `-a` | Não | `false` | Fazer backup de todos os bancos |
| `--host` | `-h` | Não | `localhost` | Host do banco |
| `--port` | `-p` | Não | `5432` | Porta do banco |
| `--username` | `-U` | Não | — | Nome de usuário |
| `--password` | `-W` | Não | solicitar | Senha |
| `--output` | `-o` | Não | `./` | Diretório de saída |

### Restauração

```bash
devmaid database restore [<banco> [<arquivo>]] [opções]
devmaid database restore --all [opções]
```

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `<banco>` | — | Sim (sem `--all`) | — | Banco de destino |
| `<arquivo>` | — | Não | `<banco>.dump` | Caminho do arquivo dump |
| `--all` | `-a` | Não | `false` | Restaurar todos os arquivos `.dump` |
| `--directory` | `-d` | Não | `./` | Diretório com arquivos `.dump` |
| `--host` | `-h` | Não | `localhost` | Host do banco |
| `--port` | `-p` | Não | `5432` | Porta do banco |
| `--username` | `-U` | Não | — | Nome de usuário |
| `--password` | `-W` | Não | solicitar | Senha |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Operação concluída com sucesso |
| `1` | Erro de banco ou arquivo |
| `2` | Argumento obrigatório ausente |
| `3` | `pg_dump` ou `pg_restore` não encontrado no PATH |

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| `pg_dump` não encontrado | Sair `3`, mensagem com instruções de instalação |
| `pg_restore` não encontrado | Sair `3`, mensagem com instruções de instalação |
| Credenciais inválidas | Sair `1`, mensagem de erro de autenticação específica |
| Arquivo dump não encontrado | Sair `1`, `"Arquivo '<caminho>' não encontrado."` |
| Diretório de saída não encontrado | Sair `1`, `"Diretório de saída '<caminho>' não existe."` |
| Nenhum arquivo `.dump` no diretório | Sair `0` com aviso: `"Nenhum arquivo .dump encontrado em '<diretório>'."` |
| Banco já existe na restauração | Registrar aviso, prosseguir com a restauração (não abortar) |

---

## Requisitos Não Funcionais

- O progresso deve ser impresso no stdout durante backup e restauração para operações que excedam 1 segundo.
- Cada operação de banco (backup ou restauração) deve ser concluída de forma independente; uma falha em um banco no modo `--all` não deve interromper os bancos restantes.
- O resumo final deve reportar: total tentado, com sucesso, com falha.
