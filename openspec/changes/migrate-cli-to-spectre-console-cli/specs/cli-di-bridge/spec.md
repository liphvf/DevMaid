## ADICIONADO Requisitos

### Requisito: TypeRegistrar faz a ponte entre Microsoft.DI e Spectre.Console.Cli
O sistema DEVE implementar `TypeRegistrar` e `TypeResolver` em `FurLab.CLI/Infrastructure/` para integrar o container `Microsoft.Extensions.DependencyInjection` com o mecanismo de resolução de tipos do `Spectre.Console.Cli`.

#### Cenário: CommandApp recebe o TypeRegistrar
- **QUANDO** o `CommandApp` é instanciado em `Program.cs`
- **ENTÃO** DEVE receber um `TypeRegistrar` construído com a `IServiceCollection` já populada via `AddFurLabServices()`

#### Cenário: TypeRegistrar constrói o ServiceProvider ao Build
- **QUANDO** o Spectre.Console.Cli solicita a construção do resolver via `ITypeRegistrar.Build()`
- **ENTÃO** o `TypeRegistrar` DEVE chamar `services.BuildServiceProvider()` e retornar um `TypeResolver` encapsulando esse provider

#### Cenário: TypeResolver resolve serviços registrados
- **QUANDO** o Spectre.Console.Cli instancia um command via `ITypeResolver.Resolve(type)`
- **ENTÃO** o `TypeResolver` DEVE retornar a instância resolvida pelo `IServiceProvider`, incluindo todas as dependências injetadas no construtor do command

#### Cenário: Commands com dependências são resolvidos pelo container
- **QUANDO** um command como `DatabaseBackupCommand` declara `IDatabaseService` e `IConfigurationService` no construtor
- **ENTÃO** o framework DEVE injetar as implementações registradas automaticamente, sem nenhuma chamada manual a `SetServiceProvider`

---

### Requisito: As cinco classes de façade estáticas são removidas
O sistema NÃO DEVE conter as classes `CLI.Services.Logger`, `CLI.Services.ConfigurationService`, `CLI.Services.UserConfigService`, `CLI.Services.CredentialService` e `CLI.Services.PostgresDatabaseLister` após a migração.

#### Cenário: Nenhum command usa SetServiceProvider
- **QUANDO** o projeto é compilado após a migração
- **ENTÃO** NÃO DEVE existir nenhuma chamada a `SetServiceProvider(sp)` em nenhum arquivo do projeto `FurLab.CLI`

#### Cenário: Nenhum command acessa serviço via service locator
- **QUANDO** um command necessita de um serviço
- **ENTÃO** o serviço DEVE estar presente como campo privado inicializado pelo construtor, não via chamada estática a uma façade
