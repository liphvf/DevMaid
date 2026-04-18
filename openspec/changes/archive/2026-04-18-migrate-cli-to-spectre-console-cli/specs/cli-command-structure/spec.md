## ADICIONADO Requisitos

### Requisito: Cada leaf command é uma classe própria herdando de AsyncCommand
O sistema DEVE implementar cada leaf command como uma classe não-estática herdando de `AsyncCommand<TSettings>` onde `TSettings` herda de `CommandSettings`, seguindo o modelo do `Spectre.Console.Cli`.

#### Cenário: Command declarado com atributos
- **QUANDO** um command é definido no CLI
- **ENTÃO** suas opções DEVEM ser declaradas com `[CommandOption("-x|--long")]`, seus argumentos com `[CommandArgument(position, "<name>")]`, suas descrições com `[Description("...")]` e seus valores padrão com `[DefaultValue(value)]`

#### Cenário: Command recebe dependências por construtor
- **QUANDO** um command necessita de um serviço do Core (ex: `IUserConfigService`, `IDatabaseService`)
- **ENTÃO** o serviço DEVE ser recebido como parâmetro do construtor (constructor injection), não por service locator estático

#### Cenário: Settings aninhadas dentro da classe de command
- **QUANDO** um command possui um conjunto de opções específico
- **ENTÃO** a classe `Settings` DEVE ser declarada como classe interna pública dentro da classe de command

#### Cenário: Execução assíncrona
- **QUANDO** a lógica de um command envolve operações I/O (banco de dados, processos externos, arquivos)
- **ENTÃO** o command DEVE herdar de `AsyncCommand<TSettings>` e implementar `ExecuteAsync(CommandContext, TSettings)` retornando `Task<int>`

---

### Requisito: Hierarquia de commands preservada identicamente
O sistema DEVE manter a mesma hierarquia de commands e subcommands da implementação atual, garantindo compatibilidade 100% com scripts e usuários existentes.

#### Cenário: Command de nível superior funciona como antes
- **QUANDO** o usuário executa `fur <command>` com os mesmos argumentos de antes da migração
- **ENTÃO** o comportamento e a saída DEVEM ser idênticos

#### Cenário: Subcommand de nível profundo funciona como antes
- **QUANDO** o usuário executa `fur database pgpass add <banco>` com os mesmos argumentos
- **ENTÃO** o comportamento e a saída DEVEM ser idênticos aos da implementação anterior

#### Cenário: Help automático gerado pelo framework
- **QUANDO** o usuário executa `fur --help`, `fur <command> --help` ou `fur <command> <subcommand> --help`
- **ENTÃO** o CLI DEVE exibir descrições, argumentos e opções derivados dos atributos declarativos das classes de command

---

### Requisito: Estrutura de arquivos reflete a hierarquia de commands
O sistema DEVE organizar os arquivos de command em subpastas que espelham a hierarquia do CLI.

#### Cenário: Navegação por grupo de command
- **QUANDO** um desenvolvedor precisa localizar o código do command `fur database backup`
- **ENTÃO** o arquivo DEVE estar em `FurLab.CLI/Commands/Database/DatabaseBackupCommand.cs`

#### Cenário: Um arquivo por leaf command
- **QUANDO** existe um leaf command (command executável, sem subcommands)
- **ENTÃO** DEVE existir exatamente um arquivo `.cs` correspondente contendo a classe de command

---

### Requisito: Commands sem DI permanecem funcionais como classes instanciáveis
O sistema DEVE converter commands que não usam DI (ex: `file combine`, `clean`, `windowsfeatures`, `winget`, `claude`, `opencode`) em classes instanciáveis sem quebrar sua lógica atual.

#### Cenário: Command sem dependências de serviço
- **QUANDO** um command não requer nenhum serviço do Core
- **ENTÃO** sua classe DEVE ter um construtor padrão (sem parâmetros) e implementar toda a lógica internamente

#### Cenário: Helpers estáticos continuam acessíveis
- **QUANDO** um command usa `SecurityUtils`, `Utils` ou `SqlQueryAnalyzer`
- **ENTÃO** essas classes DEVEM permanecer estáticas e acessíveis diretamente — sem necessidade de injeção
