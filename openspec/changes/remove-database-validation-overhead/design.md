## Contexto

O `QueryCommand` possui uma fase de "descoberta e validação" que ocorre antes da execução paralela das queries. Nessa fase, para cada servidor selecionado, o sistema:

1. Opcionalmente lista todas as databases via `SELECT datname FROM pg_database` (`ListDatabasesAsync`)
2. **Para cada database descoberta/configurada**, abre uma conexão separada e executa `SELECT 1` para verificar acessibilidade (`ValidateDatabaseAccessAsync`)

O problema está no passo 2: ele gera `N` conexões extras (uma por database) antes mesmo de executar a query real, e essas conexões são sequenciais dentro do spinner. Com muitas databases ou conexões lentas, o overhead é significativo e visível para o usuário.

Além disso, o bloco do spinner passa `CancellationToken.None` hardcoded, impedindo cancelamento pelo usuário durante a fase de descoberta.

## Objetivos / Não-Objetivos

**Objetivos:**
- Eliminar o `ValidateDatabaseAccessAsync` e todas as suas chamadas
- Simplificar `GetDatabasesForServerAsync` para retornar a lista sem validar acesso
- Exibir spinner somente quando há IO real (`fetchAllDatabases: true`)
- Propagar o `CancellationToken` correto para o bloco do spinner

**Não-Objetivos:**
- Alterar a lógica de `ListDatabasesAsync` (query de listagem permanece igual)
- Alterar o comportamento de `excludePatterns`
- Alterar o fluxo de execução paralela (`Parallel.ForEachAsync`)
- Tratar erros de conexão de forma diferente do que já existe na execução

## Decisões

### Decisão 1: Remover `ValidateDatabaseAccessAsync` completamente

**Escolha:** Deletar o método e remover todas as suas chamadas.

**Alternativa considerada:** Tornar a validação opcional via config (`validateBeforeRun: true`). Descartado — adiciona complexidade sem benefício claro; o comportamento correto é confiar na execução para detectar falhas.

**Rationale:** A validação prévia é redundante. Se um banco não está acessível, a query real vai falhar e o erro será capturado pelo log de erros existente. Não há informação nova sendo gerada pela validação.

---

### Decisão 2: Spinner condicional por `fetchAllDatabases`

**Escolha:** Exibir `AnsiConsole.Status()` somente quando `fetchAllDatabases: true` em pelo menos um servidor selecionado. Para `false`, resolver databases direto da config em memória e prosseguir.

**Alternativa considerada:** Manter spinner sempre. Descartado — exibir spinner para operação puramente em memória é enganoso para o usuário.

**Rationale:** O spinner existe para indicar espera de IO. Quando não há IO, não deve aparecer.

---

### Decisão 3: Propagação do `CancellationToken`

**Escolha:** O `CancellationToken` disponível no contexto do comando deve ser passado para `GetDatabasesForServerAsync` dentro do bloco do spinner, substituindo `CancellationToken.None`.

**Rationale:** Com conexões lentas (exatamente o cenário que justifica o spinner), o usuário deve conseguir cancelar a operação com Ctrl+C. O token já existe no escopo do comando.

## Riscos / Trade-offs

- **[Risco] Database inacessível não é detectada antes da execução** → Mitigação: o log de erros existente já captura falhas de execução por database; o comportamento de erro não muda, apenas o momento em que é detectado.
- **[Trade-off] Sem "pre-flight check"**: em cenários onde o usuário quer verificar conectividade antes de rodar uma query pesada, não há mais essa proteção → aceitável dado que o FurLab é uma ferramenta CLI de uso intencional.

## Plano de Migração

Não há migração de dados ou breaking change na interface CLI. A mudança é interna ao `QueryCommand`. Nenhum rollback especial necessário — reverter o commit restaura o comportamento anterior.

## Questões em Aberto

_(nenhuma)_
