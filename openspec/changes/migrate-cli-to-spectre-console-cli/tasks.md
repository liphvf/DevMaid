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

- [ ] 2.1 Adicionar pacote `Spectre.Console.Cli` ao `FurLab.CLI.csproj`
- [ ] 2.2 Criar pasta `FurLab.CLI/Infrastructure/`
- [ ] 2.3 Criar `FurLab.CLI/Infrastructure/TypeRegistrar.cs` implementando `ITypeRegistrar` (recebe `IServiceCollection`, constrói `ServiceProvider` no `Build()`)
- [ ] 2.4 Criar `FurLab.CLI/Infrastructure/TypeResolver.cs` implementando `ITypeResolver` (encapsula `IServiceProvider`, resolve via `GetService`)
- [ ] 2.5 Reescrever `FurLab.CLI/Program.cs` para usar `CommandApp` com `TypeRegistrar`, `app.Configure(...)`, `SetExceptionHandler` e `RunAsync(args)` — manter o antigo comentado temporariamente
- [ ] 2.6 Configurar `SetExceptionHandler` com mapeamento completo de exit codes: `NpgsqlException {SqlState}` → 10, `NpgsqlException` → 11, `IOException` → 20, `DirectoryNotFoundException` → 21, `FileNotFoundException` → 22, `UnauthorizedAccessException` → 30, `InvalidOperationException` → 40, `ArgumentException` → 41, `TimeoutException` → 50, `OperationCanceledException` → 130, `Exception` → 1
- [ ] 2.7 Configurar `config.SetApplicationName("fur")` no `CommandApp`

## 3. Migração dos Commands sem DI

- [ ] 3.1 Criar pasta `FurLab.CLI/Commands/File/` e criar `FileCombineCommand.cs` herdando de `AsyncCommand<FileCombineCommand.Settings>` com as opções `--input/-i` e `--output/-o`
- [ ] 3.2 Criar pasta `FurLab.CLI/Commands/Clean/` e criar `CleanCommand.cs` herdando de `AsyncCommand<CleanCommand.Settings>` com argumento opcional `[directory]`
- [ ] 3.3 Criar pasta `FurLab.CLI/Commands/Winget/` e criar `WingetBackupCommand.cs` com opção `--output/-o`
- [ ] 3.4 Criar `FurLab.CLI/Commands/Winget/WingetRestoreCommand.cs` com opção `--input/-i`
- [ ] 3.5 Criar pasta `FurLab.CLI/Commands/WindowsFeatures/` e criar `WindowsFeaturesExportCommand.cs` com argumento `<path>`
- [ ] 3.6 Criar `FurLab.CLI/Commands/WindowsFeatures/WindowsFeaturesImportCommand.cs` com argumento `<path>`
- [ ] 3.7 Criar `FurLab.CLI/Commands/WindowsFeatures/WindowsFeaturesListCommand.cs` com opção `--enabled-only`
- [ ] 3.8 Criar pasta `FurLab.CLI/Commands/Claude/` e criar `ClaudeInstallCommand.cs`
- [ ] 3.9 Criar pasta `FurLab.CLI/Commands/Claude/Settings/` e criar `ClaudeMcpDatabaseCommand.cs` e `ClaudeWinEnvCommand.cs`
- [ ] 3.10 Criar pasta `FurLab.CLI/Commands/OpenCode/Settings/` e criar `OpenCodeMcpDatabaseCommand.cs`
- [ ] 3.11 Criar `OpenCodeDefaultModelCommand.cs` com argumento opcional `[model-id]` e opção `--global`; migrar o hook `internal static Func<IReadOnlyList<string>>? ModelsProvider` para campo interno da classe
- [ ] 3.12 Registrar todos esses commands no `app.Configure(...)` do `Program.cs` com a hierarquia correta de `AddBranch`/`AddCommand`

## 4. Migração dos Commands com DI

- [ ] 4.1 Criar pasta `FurLab.CLI/Commands/Docker/` e criar `DockerPostgresCommand.cs` recebendo `IDockerService` por construtor
- [ ] 4.2 Criar pasta `FurLab.CLI/Commands/Database/` e criar `DatabaseBackupCommand.cs` recebendo `IDatabaseService`, `IConfigurationService`, `IPostgresBinaryLocator` e `IPostgresPasswordHandler` por construtor; migrar todas as opções com atributos declarativos (20+ opções/argumentos)
- [ ] 4.3 Criar `DatabaseRestoreCommand.cs` com as mesmas dependências e opções equivalentes
- [ ] 4.4 Criar pasta `FurLab.CLI/Commands/Database/PgPass/` e criar `PgPassAddCommand.cs` recebendo `IPgPassService` e `IPostgresPasswordHandler` por construtor
- [ ] 4.5 Criar `PgPassListCommand.cs` recebendo `IPgPassService` por construtor
- [ ] 4.6 Criar `PgPassRemoveCommand.cs` recebendo `IPgPassService` por construtor
- [ ] 4.7 Criar pasta `FurLab.CLI/Commands/Settings/DbServers/` e criar `DbServersListCommand.cs` recebendo `IUserConfigService` por construtor
- [ ] 4.8 Criar `DbServersAddCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IPostgresPasswordHandler` por construtor; migrar o wizard interativo Spectre.Console
- [ ] 4.9 Criar `DbServersRemoveCommand.cs` recebendo `IUserConfigService` por construtor
- [ ] 4.10 Criar `DbServersTestCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IDatabaseService` por construtor
- [ ] 4.11 Criar `DbServersSetPasswordCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `IPostgresPasswordHandler` por construtor
- [ ] 4.12 Criar pasta `FurLab.CLI/Commands/Query/` e converter `CsvExporter` de classe estática para classe instanciável (sem interface por ora); mover para `FurLab.CLI/Commands/Query/CsvExporter.cs`
- [ ] 4.13 Criar `QueryRunCommand.cs` recebendo `IUserConfigService`, `ICredentialService` e `CsvExporter` por construtor; migrar todas as 20+ opções com atributos declarativos; preservar lógica de Channels, Polly retry e execução paralela
- [ ] 4.14 Registrar todos esses commands no `app.Configure(...)` do `Program.cs` com a hierarquia correta

## 5. Remoção das Façades Estáticas e Limpeza

- [ ] 5.1 Remover `FurLab.CLI/Services/ConfigurationService.cs` (façade estática)
- [ ] 5.2 Remover `FurLab.CLI/Services/UserConfigService.cs` (façade estática)
- [ ] 5.3 Remover `FurLab.CLI/Services/CredentialService.cs` (façade estática)
- [ ] 5.4 Remover `FurLab.CLI/Services/PostgresDatabaseLister.cs` (façade estática)
- [ ] 5.5 Remover `FurLab.CLI/Services/Logging/Logger.cs` (façade estática)
- [ ] 5.6 Remover `FurLab.CLI/Services/DockerService.cs` e `DockerConstants.cs` (movidos para Core)
- [ ] 5.7 Remover `FurLab.CLI/Services/PostgresPasswordHandler.cs` (movido para Core)
- [ ] 5.8 Remover os 10 arquivos de command originais (`FileCommand.cs`, `ClaudeCodeCommand.cs`, `OpenCodeCommand.cs`, `WingetCommand.cs`, `DatabaseCommand.cs`, `PgPassCommand.cs`, `QueryCommand.cs`, `CleanCommand.cs`, `WindowsFeaturesCommand.cs`, `DockerCommand.cs`, `SettingsCommand.cs`)
- [ ] 5.9 Remover código comentado do `Program.cs` antigo e finalizar a versão limpa
- [ ] 5.10 Remover referência ao pacote `System.CommandLine` do `FurLab.CLI.csproj`
- [ ] 5.11 Verificar que não existem referências a `SetServiceProvider` em nenhum arquivo do projeto

## 6. Validação e Smoke Tests

- [ ] 6.1 Executar `dotnet build` e garantir zero erros e zero warnings de compilação
- [ ] 6.2 Testar `fur --help` — verificar que todos os 10 grupos de commands aparecem listados
- [ ] 6.3 Testar `fur file combine --help` — verificar opções `--input` e `--output`
- [ ] 6.4 Testar `fur database backup --help` — verificar todas as opções incluindo `--all` e `--exclude-table-data`
- [ ] 6.5 Testar `fur database pgpass list` — verificar que o bug do `new PgPassService()` foi eliminado (logs aparecem)
- [ ] 6.6 Testar `fur query run --help` — verificar que todas as 20+ opções aparecem
- [ ] 6.7 Testar `fur settings db-servers add` sem argumentos — verificar que o wizard interativo funciona
- [ ] 6.8 Testar `fur settings db-servers ls` — verificar saída em tabela Spectre.Console
- [ ] 6.9 Testar `fur docker postgres` — verificar integração com `IDockerService` injetado
- [ ] 6.10 Simular erro de banco (host inválido) e verificar que o exit code retornado é `10` ou `11`
- [ ] 6.11 Testar `fur clean` — verificar remoção de pastas `bin`/`obj`
- [ ] 6.12 Executar `dotnet test` no projeto `FurLab.Tests` e verificar que todos os testes existentes passam
