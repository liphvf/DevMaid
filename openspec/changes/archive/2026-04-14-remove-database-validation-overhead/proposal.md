## Por que

O fluxo de descoberta de databases realiza conexões individuais a cada banco para validar acessibilidade (`SELECT 1`) antes de executar a query real, resultando em um número excessivo de requisições ao servidor e lentidão perceptível. Essa validação prévia é desnecessária: falhas devem ser tratadas durante a execução e registradas no log.

## O que Muda

- **REMOVIDO**: `ValidateDatabaseAccessAsync` — não realiza mais validação de acesso individual por database antes da execução
- **MODIFICADO**: `GetDatabasesForServerAsync` — simplificado para dois caminhos: auto-descoberta via `pg_database` ou leitura da config; sem etapa de validação
- **MODIFICADO**: Spinner de feedback visual — exibido apenas quando `fetchAllDatabases: true` (único caso com IO real na fase de descoberta); `fetchAllDatabases: false` vai direto para execução
- **CORRIGIDO**: `CancellationToken.None` hardcoded no bloco do spinner substituído pelo token real do comando
- **MODIFICADO**: Falhas de conexão a databases durante execução são registradas no log de erros existente, sem interromper as demais

## Capacidades

### Novas Capacidades

_(nenhuma)_

### Capacidades Modificadas

- `server-auto-discover-databases`: o requisito de validação de databases acessíveis (Requirement: Validação de databases acessíveis) é removido — databases descobertas são usadas diretamente sem verificação prévia de conectividade
- `query-run-progress-feedback`: o spinner de descoberta passa a aparecer apenas quando `fetchAllDatabases: true`

## Impacto

- `FurLab.CLI/Commands/QueryCommand.cs` — remoção de `ValidateDatabaseAccessAsync`, simplificação de `GetDatabasesForServerAsync`, ajuste do bloco `AnsiConsole.Status()` e correção do `CancellationToken`
- Redução de conexões ao servidor: de `2N + 1` para `1` (auto-descoberta) ou `0` (config manual) por servidor antes da execução
