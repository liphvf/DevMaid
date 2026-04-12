## Context

FurLab é uma CLI tool .NET 10 para gerenciamento de PostgreSQL, com comandos para backup, restore, execução de queries e gerenciamento de containers Docker. Atualmente, a configuração de servidores é feita via `appsettings.json` no diretório do projeto, e o comando `query run` requer parâmetros explícitos de conexão ou usa um "servidor primário" configurado. Não há comandos CLI para gerenciar servidores configurados.

**Estado atual:**

- `appsettings.json` no projeto define servidores com conexão PostgreSQL
- `query run` requer `-i arquivo.sql` e parâmetros de conexão explícitos
- Flag `--servers` executa query em todos os servidores configurados
- Sem comando para adicionar/remover/testar servidores via CLI
- Configurações de banco de dados do usuário em `%LocalAppData%\FurLab\appsettings.json` (formato simples key=value)

**Stakeholders:**

- Desenvolvedores que usam FurLab para queries em múltiplos servidores PostgreSQL
- Ambiente de desenvolvimento (não produção)

## Goals / Non-Goals

**Goals:**

- Fluxo interativo padrão para seleção de servidores ao executar queries
- Gerenciamento completo de servidores via CLI (add, ls, rm, test)
- Execução paralela configurável com tolerância a falhas parcial
- Detecção e confirmação de queries destrutivas antes da execução
- Configuração por usuário em `%LocalAppData%\FurLab\furlab.jsonc`
- CSV consolidado com metadados de execução (server, database, timestamp, status, row count, erro)
- Auto-descoberta de databases por servidor com patterns de exclusão configuráveis

**Non-Goals:**

- Suporte a produção/enterprise (senhas em texto puro são aceitáveis para dev)
- Criptografia de senhas ou integração com credential managers
- Suporte a outros bancos além de PostgreSQL
- UI gráfica ou web
- Execução agendada de queries

## Decisions

### 1. Armazenamento em `%LocalAppData%\FurLab\furlab.jsonc`

**Decisão:** Migrar configurações de servidor do `appsettings.json` do projeto para `%LocalAppData%\FurLab\furlab.jsonc`.

**Razão:** Configurações de servidor são específicas do usuário, não do projeto. Senhas em texto puro não devem ser versionadas. JSONC permite comentários para documentação.

**Alternativas consideradas:**

- SQLite local: overkill para configurações simples
- Manter no projeto: expõe senhas no versionamento
- Windows Credential Manager: complexo demais para ambiente dev

### 2. Fluxo sempre interativo para seleção de servidores

**Decisão:** `query run` sem parâmetros de servidor sempre mostra MultiSelectionPrompt com servidores configurados (todos pré-selecionados).

**Razão:** Remove ambiguidade sobre qual servidor será usado. Usuário sempre vê e confirma os alvos antes de executar.

**Alternativas consideradas:**

- Manter servidor primário como default: menos visibilidade sobre quais servidores existem
- Flag `--servers` para todos: removido pois o novo fluxo já lida com isso

### 3. Execução paralela com limite configurável

**Decisão:** Queries em múltiplos servidores/databases rodam em paralelo via `Parallel.ForEachAsync` com `MaxDegreeOfParallelism` configurável por servidor (default: 4).

**Razão:** Performance significativamente melhor para muitos alvos. Limite configurável evita sobrecarga do PostgreSQL.

**Alternativas consideradas:**

- Sequencial: mais lento mas mais simples
- Pool de threads fixo: menos flexível

### 4. Detecção de queries destrutivas via regex simples

**Decisão:** Parser regex que identifica o primeiro keyword SQL significativo (ignorando comentários e CTEs) e compara com lista de keywords destrutivas.

**Lista de keywords destrutivas:** INSERT, UPDATE, DELETE, ALTER, DROP, CREATE, TRUNCATE, MERGE, GRANT, REVOKE, SET ROLE

**Razão:** Leve, sem dependências externas, cobre 95% dos casos de uso. Falsos positivos são aceitáveis (melhor confirmar demais que de menos).

**Alternativas consideradas:**

- Parser SQL completo: dependência pesada, complexidade desnecessária para dev tool
- Executar e reverter: não funciona para todas as queries, complexo

### 5. Tolerância a falhas parcial

**Decisão:** Se um servidor/database falha, logar erro e continuar com os próximos. CSV final inclui linhas de erro.

**Razão:** Um servidor offline não deve bloquear queries em outros. Usuário vê resultado parcial com erros claramente identificados.

### 6. CSV consolidado com metadados

**Decisão:** Formato: `Server,Database,ExecutedAt,Status,RowCount,Error,<result columns...>`

**Razão:** Permite análise pós-execução de qual servidor/database teve sucesso ou falha. Timestamp ajuda em auditoria.

### 7. Auto-descoberta de databases

**Decisão:** Query `SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true` com exclusão por patterns configuráveis por servidor.

**Razão:** Usuário não precisa manter lista manual de databases. Patterns permitem excluir system databases.

## Risks / Trade-offs

| Risco                                                 | Mitigação                                                                                                    |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Regex falso positivo para query destrutiva            | Usuário pode confirmar e executar; melhor falso positivo que falso negativo                                  |
| Regex falso negativo (query destrutiva não detectada) | Lista de keywords cobre casos comuns; usuário deve revisar queries antes de executar em múltiplos servidores |
| Senhas em texto puro no JSONC                         | Aceitável para ambiente de desenvolvimento; documentar riscos no README                                      |
| Execução paralela sobrecarrega PostgreSQL             | Limite configurável por servidor (default 4); usuário pode reduzir se necessário                             |
| Migração de appsettings.json para furlab.jsonc        | Script de migração ou instrução manual no README; ambos os formatos coexistem durante transição              |
| JSONC parsing pode falhar com sintaxe inválida        | Validação no carregamento; mensagem de erro clara com linha/coluna do erro                                   |

## Migration Plan

1. **Fase 1:** Novo comando `settings db-servers` lê/escreve em `furlab.jsonc`
2. **Fase 2:** `query run` migra para usar `furlab.jsonc` como fonte primária
3. **Fase 3:** Se `appsettings.json` existir, mostrar warning sugerindo migração
4. **Fase 4:** Remover completamente suporte a `appsettings.json` para servidores (próxima major version)

**Rollback:** Manter `appsettings.json` intacto durante transição. Usuário pode reverter removendo `furlab.jsonc`.

## Decisões de Design (Open Questions Resolvidas)

1. **Dry-run com EXPLAIN:** Não é necessário row count preciso. O EXPLAIN mostra o plano de execução estimado, que é suficiente para o usuário entender o impacto antes de confirmar. Implementar `SELECT count(*)` prévio adicionaria complexidade desnecessária para queries complexas.

2. **Output de erro no CSV:** Uma linha por database que falhou. Isso permite rastrear exatamente qual database em qual servidor teve problema, facilitando debugging e análise pós-execução.

3. **Validação de input no `add -i`:** Validação de host/porta ocorre apenas no `test`, não no `add`. Isso permite salvar configurações mesmo quando o servidor está temporariamente indisponível, e separa claramente a responsabilidade de "salvar configuração" de "verificar conectividade".
