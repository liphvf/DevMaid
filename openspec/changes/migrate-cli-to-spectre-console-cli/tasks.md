## 1. Preparação do Core — Novos Serviços e Interfaces

- [x] 1.1 Criar interface `IPostgresBinaryLocator` em `FurLab.Core/Services/` com métodos `FindPgDump()`, `FindPgRestore()`, `FindPsql()`
- [x] 1.2 Modificar `PostgresBinaryLocator` para implementar `IPostgresBinaryLocator` (manter lógica existente, apenas adicionar interface)
- [x] 1.3 Criar interface `IPostgresPasswordHandler` em `FurLab.Core/Services/` com método `ReadPasswordInteractively(string prompt)`
- [x] 1.4 Mover `PostgresPasswordHandler.cs` de `FurLab.CLI/Services/` para `FurLab.Core/Services/` e fazer implementar `IPostgresPasswordHandler`
- [x] 1.5 Criar interface `IDockerService` em `FurLab.Core/Services/Docker/` com métodos `GetDockerStatusAsync()` e `EnsurePostgresContainerAsync(...)`
- [x] 1.6 Mover `DockerService.cs` de `FurLab.CLI/Services/` para `FurLab.Core/Services/Docker/` e fazer implementar `IDockerService`
- [x] 1.7 Mover `DockerConstants.cs` de `FurLab.CLI/Services/` para `FurLab.Core/Services/Docker/`
- [x] 1.8 Registrar `IPostgresBinaryLocator`, `IPostgresPasswordHandler` e `IDockerService` como Singleton em `ServiceCollectionExtensions.AddFurLabServices()`
- [x] 1.9 Atualizar `PostgresBinaryHealthCheck` para receber `IPostgresBinaryLocator` por construtor em vez de chamar a classe estática diretamente
- [x] 1.10 Verificar que o projeto `FurLab.Core` compila sem erros após as mudanças

## 2. Infraestrutura CLI — Spectre.Console.Cli e TypeRegistrar

- [x] 2.1 Adicionar pacote `Spectre.Console.Cli` ao `FurLab.CLI.csproj`
- [x] 2.2 Criar pasta `FurLab.CLI/Infrastructure/`
- [x] 2.3 Criar `FurLab.CLI/Infrastructure/TypeRegistrar.cs` implementando `ITypeRegistrar` (recebe `IServiceCollection`, constrói `ServiceProvider` no `Build()`)
- [x] 2.4 Criar `FurLab.CLI/Infrastructure/TypeResolver.cs` implementando `ITypeResolver` (encapsula `IServiceProvider`, resolve via `GetService`)
- [x] 2.5 Reescrever `FurLab.CLI/Program.cs` para usar `CommandApp` com `TypeRegistrar`, `app.Configure(...)`, `SetExceptionHandler` e `RunAsync(args)`
- [x] 2.6 Configurar `SetExceptionHandler` com mapeamento completo de exit codes: `NpgsqlException {SqlState}` → 10, `NpgsqlException` → 11, `IOException` → 20, `DirectoryNotFoundException` → 21, `FileNotFoundException` → 22, `UnauthorizedAccessException` → 30, `InvalidOperationException` → 40, `ArgumentException` → 41, `TimeoutException` → 50, `OperationCanceledException` → 130, `Exception` → 1
- [x] 2.7 Configurar `config.SetApplicationName("fur")` no `CommandApp`

## 3. Migração dos Commands sem DI

- [x] 3.1 Criar `Commands/FileUtils/FileCombineCommand.cs` herdando de `AsyncCommand<FileCombineCommand.Settings>` com as opções `--input/-i` e `--output/-o`
- [x] 3.2 Criar `Commands/Clean/CleanCommand.cs` herdando de `AsyncCommand<CleanCommand.Settings>` com argumento opcional `[directory]`
- [x] 3.3 Criar `Commands/Winget/WingetBackupCommand.cs` com opção `--output/-o`
- [x] 3.4 Criar `Commands/Winget/WingetRestoreCommand.cs` com opção `--input/-i`
- [x] 3.5 Criar `Commands/WindowsFeatures/WindowsFeaturesExportCommand.cs` com argumento `<path>`
- [x] 3.6 Criar `Commands/WindowsFeatures/WindowsFeaturesImportCommand.cs` com argumento `<path>`
- [x] 3.7 Criar `Commands/WindowsFeatures/WindowsFeaturesListCommand.cs` com opção `--enabled-only`
- [x] 3.8 Criar `Commands/Claude/ClaudeInstallCommand.cs`
- [x] 3.9 Criar `Commands/Claude/Settings/ClaudeMcpDatabaseCommand.cs` e `ClaudeWinEnvCommand.cs`
- [x] 3.10 Criar `Commands/OpenCode/Settings/OpenCodeMcpDatabaseCommand.cs`
- [x] 3.11 Criar `OpenCodeDefaultModelCommand.cs` com argumento opcional `[model-id]` e opção `--global`; hook `ModelsProvider` migrado para campo interno
- [x] 3.12 Registrar todos os commands no `app.Configure(...)` do `Program.cs` com a hierarquia correta de `AddBranch`/`AddCommand`

## 4. Migração dos Commands com DI

- [x] 4.1 Criar `Commands/Docker/DockerPostgresCommand.cs` recebendo `IDockerService` por construtor
- [x] 4.2 Criar `Commands/Database/DatabaseBackupCommand.cs` recebendo `IDatabaseService`, `IConfigurationService`, `IPostgresBinaryLocator` e `IPostgresPasswordHandler` por construtor; 15+ opções com atributos declarativos
- [x] 4.3 Criar `Commands/Database/DatabaseRestoreCommand.cs` com as mesmas dependências e opções equivalentes
- [x] 4.4 Criar `Commands/Database/PgPass/PgPassAddCommand.cs` recebendo `IPgPassService` e `IPostgresPasswordHandler` por construtor
- [x] 4.5 Criar `Commands/Database/PgPass/PgPassListCommand.cs` recebendo `IPgPassService` por construtor
- [x] 4.6 Criar `Commands/Database/PgPass/PgPassRemoveCommand.cs` recebendo `IPgPassService` por construtor
- [x] 4.7 Criar `Commands/Settings/DbServers/DbServersListCommand.cs` recebendo `IUserConfigService` por construtor
- [x] 4.8 Criar `Commands/Settings/DbServers/DbServersAddCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IPostgresPasswordHandler` por construtor; wizard interativo Spectre.Console preservado
- [x] 4.9 Criar `Commands/Settings/DbServers/DbServersRemoveCommand.cs` recebendo `IUserConfigService` por construtor
- [x] 4.10 Criar `Commands/Settings/DbServers/DbServersTestCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IDatabaseService` por construtor
- [x] 4.11 Criar `Commands/Settings/DbServers/DbServersSetPasswordCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IPostgresPasswordHandler` por construtor
- [x] 4.12 Criar `Commands/Query/CsvExporter.cs` convertido de classe estática para instanciável (sem interface); `CsvRow` e `ExecutionLogEntry` também movidos para `Commands/Query/`
- [x] 4.13 Criar `Commands/Query/QueryRunCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `CsvExporter` por construtor; 20+ opções com atributos declarativos; lógica de Channels, Polly retry e execução paralela preservada
- [x] 4.14 Registrar todos os commands com DI no `app.Configure(...)` do `Program.cs` (resolvidos via `TypeRegistrar`/`TypeResolver`)

## 5. Remoção das Façades Estáticas e Limpeza

- [x] 5.1 Remover `FurLab.CLI/Services/ConfigurationService.cs` (façade estática)
- [x] 5.2 Remover `FurLab.CLI/Services/UserConfigService.cs` (façade estática)
- [x] 5.3 Remover `FurLab.CLI/Services/CredentialService.cs` (façade estática)
- [x] 5.4 Remover `FurLab.CLI/Services/PostgresDatabaseLister.cs` (façade estática)
- [x] 5.5 Remover `FurLab.CLI/Services/Logging/Logger.cs` (façade estática)
- [x] 5.6 Mover `DockerService.cs` e `DockerConstants.cs` de `FurLab.CLI/Services/` para `FurLab.Core/Services/Docker/` (também `DockerStatus.cs` e `DockerOperationException.cs`)
- [x] 5.7 Mover `PostgresPasswordHandler.cs` de `FurLab.CLI/Services/` para `FurLab.Core/Services/`
- [x] 5.8 Remover os 10+ arquivos de command originais (`FileCommand.cs`, `ClaudeCodeCommand.cs`, `OpenCodeCommand.cs`, `WingetCommand.cs`, `DatabaseCommand.cs`, `PgPassCommand.cs`, `QueryCommand.cs`, `CleanCommand.cs`, `WindowsFeaturesCommand.cs`, `DockerCommand.cs`, `SettingsCommand.cs`) e os arquivos `CommandOptions/`
- [x] 5.9 Remover código comentado do `Program.cs` antigo; `Program` alterado de `static class` para `class` não-estática
- [x] 5.10 Remover referência ao pacote `System.CommandLine` do `FurLab.CLI.csproj`
- [x] 5.11 Verificar que não existem referências a `SetServiceProvider` em nenhum arquivo fonte do projeto
- [x] 5.12 Mover `Utils.cs` para `FurLab.CLI/Utils/Utils.cs` (namespace `FurLab.CLI.Utils` alinhado com a pasta)
- [x] 5.13 Mover `FurLabConstants.cs`, `FurLabExceptions.cs`, `SqlQueryAnalyzer.cs`, `QueryType.cs` de `FurLab.CLI/Services/` para `FurLab.CLI/Utils/` (namespace `FurLab.CLI.Utils`)
- [x] 5.14 Atualizar arquivos de teste — remover testes baseados em `System.CommandLine` (4 arquivos obsoletos excluídos), atualizar namespaces e referências nos testes restantes

## 6. Validação e Smoke Tests

- [x] 6.1 Executar `dotnet build` — zero erros e zero warnings de compilação ✅
- [x] 6.2 Testar `fur --help` — verificar que todos os 10 grupos de commands aparecem listados
- [ ] 6.3 Testar `fur file combine --help` — verificar opções `--input` e `--output`
- [x] 6.4 Testar `fur database backup --help` — verificar todas as opções incluindo `--all` e `--exclude-table-data`
- [x] 6.5 Testar `fur database pgpass list` — verificar que o bug do `new PgPassService()` foi eliminado (logs aparecem)
- [x] 6.6 Testar `fur query run --help` — verificar que todas as 20+ opções aparecem
- [x] 6.7 Testar `fur settings db-servers add` sem argumentos — verificar que o wizard interativo funciona
- [x] 6.8 Testar `fur settings db-servers ls` — verificar saída em tabela Spectre.Console
- [x] 6.9 Testar `fur docker postgres` — verificar integração com `IDockerService` injetado
- [ ] 6.10 Simular erro de banco (host inválido) e verificar que o exit code retornado é `10` ou `11`
- [x] 6.11 Testar `fur clean` — verificar remoção de pastas `bin`/`obj`
- [x] 6.12 Executar `dotnet test` no projeto `FurLab.Tests` — 124 testes passam ✅
