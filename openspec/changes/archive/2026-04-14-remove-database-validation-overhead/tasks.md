## 1. Remoção da Validação

- [x] 1.1 Remover o método `ValidateDatabaseAccessAsync` de `QueryCommand.cs`
- [x] 1.2 Remover todas as chamadas a `ValidateDatabaseAccessAsync` dentro de `GetDatabasesForServerAsync`

## 2. Simplificação de `GetDatabasesForServerAsync`

- [x] 2.1 Refatorar `GetDatabasesForServerAsync` para retornar a lista de databases sem etapa de validação: `FetchAllDatabases = true` chama `ListDatabasesAsync` diretamente; `FetchAllDatabases = false` retorna `server.Databases` da config
- [x] 2.2 Manter o tratamento de fallback para falha em `ListDatabasesAsync` (usa `server.Databases` se disponível)

## 3. Ajuste do Spinner de Descoberta

- [x] 3.1 Tornar o bloco `AnsiConsole.Status()` condicional: exibir apenas quando pelo menos um servidor selecionado tem `fetchAllDatabases: true`
- [x] 3.2 Atualizar a mensagem do spinner para "Discovering databases on [servidor]..." (sem "validating")
- [x] 3.3 Substituir `CancellationToken.None` pelo token real do comando no bloco do spinner e na chamada a `GetDatabasesForServerAsync`
