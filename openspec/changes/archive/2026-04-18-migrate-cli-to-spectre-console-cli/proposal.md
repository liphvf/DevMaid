## Por que

O projeto `FurLab.CLI` utiliza `System.CommandLine` para parsing de argumentos e um padrão de **service locator estático** (cinco classes façade que armazenam um `IServiceProvider` e o consultam em tempo de execução) para acessar os serviços do `FurLab.Core`. Esse padrão nega os benefícios do DI real, impede testes unitários isolados dos commands, e viola a intenção original do `FurLab.Core` (ser um núcleo compartilhável por múltiplos frontends). A migração para `Spectre.Console.Cli` resolve essa dívida técnica, adota um framework de CLI idiomático e maduro, e prepara a base para a criação futura de um frontend (web ou desktop) que reutilize o Core sem nenhuma mudança.

## O que Muda

- **REMOVIDO**: dependência `System.CommandLine` do projeto `FurLab.CLI`
- **ADICIONADO**: dependência `Spectre.Console.Cli` no projeto `FurLab.CLI`
- **REMOVIDO**: as cinco classes de façade estáticas (`CLI.Services.Logger`, `CLI.Services.ConfigurationService`, `CLI.Services.UserConfigService`, `CLI.Services.CredentialService`, `CLI.Services.PostgresDatabaseLister`) — o DI passa a ser por injeção de construtor nos commands
- **REMOVIDO**: padrão `static class Command { static Command Build() { ... } }` para todos os 10 commands existentes
- **ADICIONADO**: cada command e subcommand vira uma classe própria herdando de `AsyncCommand<Settings>` ou `Command<Settings>`, com atributos `[CommandOption]`, `[CommandArgument]`, `[Description]`, `[DefaultValue]`
- **ADICIONADO**: `TypeRegistrar` e `TypeResolver` em `FurLab.CLI/Infrastructure/` para fazer a ponte entre `Microsoft.Extensions.DependencyInjection` e `Spectre.Console.Cli`
- **ADICIONADO**: `SetExceptionHandler` no `CommandApp` para mapeamento centralizado de exceções a exit codes específicos, substituindo o bloco `try/catch` manual do `Program.cs` atual
- **MOVIDO**: `DockerService` e `DockerConstants` do `FurLab.CLI` para `FurLab.Core`, com interface `IDockerService`
- **MOVIDO**: `PostgresPasswordHandler` do `FurLab.CLI` para `FurLab.Core`, com interface `IPostgresPasswordHandler`
- **MODIFICADO**: `PostgresBinaryLocator` no `FurLab.Core` passa a ter interface `IPostgresBinaryLocator` e é registrado no container de DI (hoje é chamado diretamente como classe estática)
- **MANTIDO**: toda a interface pública do CLI (`fur database backup`, `fur query run`, etc.) — a migração é 100% transparente para o usuário
- **MANTIDO**: `SecurityUtils`, `Utils`, `SqlQueryAnalyzer` como classes estáticas em `FurLab.CLI/Utils/` — são helpers puros sem efeitos colaterais
- **MANTIDO**: `FurLab.Core` sem mudanças nas interfaces existentes (`IConfigurationService`, `IUserConfigService`, `ICredentialService`, `IDatabaseService`, `IPgPassService`)

## Capacidades

### Novas Capacidades

- `cli-command-structure`: Estrutura de commands do CLI usando `Spectre.Console.Cli` com herança de `AsyncCommand<Settings>`, atributos declarativos e DI por construtor
- `cli-di-bridge`: Integração entre `Microsoft.Extensions.DependencyInjection` e `Spectre.Console.Cli` via `TypeRegistrar`/`TypeResolver`
- `cli-exception-handler`: Tratamento centralizado de exceções com mapeamento para exit codes via `SetExceptionHandler`
- `core-docker-service`: Serviço de gerenciamento de containers Docker no `FurLab.Core` com interface `IDockerService`
- `core-postgres-binary-locator`: Localizador de binários PostgreSQL no `FurLab.Core` com interface `IPostgresBinaryLocator`
- `core-postgres-password-handler`: Handler de leitura de senha PostgreSQL interativa no `FurLab.Core` com interface `IPostgresPasswordHandler`

### Capacidades Modificadas

_(nenhuma — as interfaces existentes do Core não mudam seus requisitos, apenas ganham novas implementações registradas)_

## Impacto

**FurLab.CLI**
- `Program.cs`: completamente reescrito — substitui `RootCommand` (System.CommandLine) por `CommandApp` (Spectre.Console.Cli) com `TypeRegistrar`, `app.Configure(...)` e `SetExceptionHandler`
- `Commands/`: todos os 10 arquivos de command reescritos — cada um desmembrado em N classes de command (uma por leaf command), organizadas em subpastas por grupo
- `Services/`: as cinco classes de façade (`ConfigurationService.cs`, `UserConfigService.cs`, `CredentialService.cs`, `PostgresDatabaseLister.cs`, `Logging/Logger.cs`) removidas
- `Services/DockerService.cs`, `Services/DockerConstants.cs`: movidos para `FurLab.Core`
- `Services/PostgresPasswordHandler.cs`: movido para `FurLab.Core`
- Nova pasta `Infrastructure/` com `TypeRegistrar.cs` e `TypeResolver.cs`
- `Utils/`: `SecurityUtils.cs`, `Utils.cs`, `SqlQueryAnalyzer.cs` mantidos como estáticos sem mudança

**FurLab.Core**
- `Services/ServiceCollectionExtensions.cs`: adiciona registro de `IDockerService`, `IPostgresBinaryLocator`, `IPostgresPasswordHandler`
- `Services/Docker/IDockerService.cs` + `DockerService.cs`: novos (movidos do CLI)
- `Services/Docker/DockerConstants.cs`: novo (movido do CLI)
- `Services/PostgresBinaryLocator.cs`: adiciona interface `IPostgresBinaryLocator`
- `Services/PostgresPasswordHandler.cs` + `IPostgresPasswordHandler.cs`: novos (movidos do CLI)

**FurLab.Tests**
- Testes dos commands passam a usar mocks das interfaces (agora possível com DI real)

**Dependências de pacote**
- `FurLab.CLI`: remove `System.CommandLine`, adiciona `Spectre.Console.Cli`
- `FurLab.Core`: sem mudança de dependências de pacote
