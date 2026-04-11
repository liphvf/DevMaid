## 1. Correção em DatabaseCommand.cs

- [x] 1.1 Adicionar validação `SecurityUtils.IsValidPostgreSQLIdentifier(databaseName)` em `CreateDatabaseIfNeeded` antes da query SQL
- [x] 1.2 Se inválido, lançar `ArgumentException` com mensagem clara (ex: "Invalid database name")
- [x] 1.3 Verificar se `BuildPgDumpArguments` e `BuildPgRestoreArguments` precisam de validação similar
  - **Resultado**: Não precisa. `databaseName` é passado como `-d "{options.DatabaseName}"` no arguments, não em SQL query. O pg_dump/pg_restore treating it as an identifier, not a SQL string.

## 2. Correção em DatabaseService.cs

- [x] 2.1 Adicionar validação `Regex.IsMatch(databaseName, @"^[a-zA-Z_][a-zA-Z0-9_]*$")` em `CreateDatabaseIfNeededAsync` antes da query SQL
- [x] 2.2 Se inválido, lançar `ArgumentException` com mensagem clara
- [x] 2.3 Verificar se `BuildPgDumpArguments` e `BuildPgRestoreArguments` precisam de validação similar
  - **Resultado**: Não precisa pelo mesmo motivo acima.

## 3. Verificação

- [x] 3.1 Compilar o projeto (`dotnet build`) - **Sucesso: 0 Warnings, 0 Errors**
- [x] 3.2 Executar testes existentes (`dotnet test`) - **Sucesso: 117 Passed, 2 Skipped**
- [ ] 3.3 Testar manualmente com nome inválido (ex: `test'db`) deve lançar exceção
- [ ] 3.4 Testar manualmente com nome válido (ex: `test_db`) deve funcionar normalmente
