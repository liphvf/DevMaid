## Propósito

Define a estrutura de comandos do CLI usando Spectre.Console.Cli com herança adequada, atributos declarativos e suporte a injeção de dependência.

## Requisitos

### Requisito: Cada comando folha é uma classe própria herdando de AsyncCommand
O sistema DEVE implementar cada comando folha como uma classe não-estática herdando de `AsyncCommand<TSettings>` onde `TSettings` herda de `CommandSettings`, seguindo o modelo do Spectre.Console.Cli.

#### Cenário: Comando declarado com atributos
- **QUANDO** um comando é definido no CLI
- **ENTÃO** suas opções DEVEM ser declaradas com `[CommandOption("-x|--long")]`, seus argumentos com `[CommandArgument(position, "<name>")]`, descrições com `[Description("...")]`, e valores padrão com `[DefaultValue(value)]`

#### Cenário: Comando recebe dependências via construtor
- **QUANDO** um comando precisa de um serviço do Core (ex: `IUserConfigService`, `IDatabaseService`)
- **ENTÃO** o serviço DEVE ser recebido como parâmetro do construtor (injeção via construtor), não via service locator estático

#### Cenário: Settings aninhadas dentro da classe de comando
- **QUANDO** um comando possui um conjunto específico de opções
- **ENTÃO** a classe `Settings` DEVE ser declarada como classe interna pública dentro da classe de comando

#### Cenário: Execução assíncrona
- **QUANDO** a lógica de um comando envolve operações de I/O (bancos de dados, processos externos, arquivos)
- **ENTÃO** o comando DEVE herdar de `AsyncCommand<TSettings>` e implementar `ExecuteAsync(CommandContext, TSettings)` retornando `Task<int>`

---

### Requisito: Hierarquia de comandos preservada identicamente
O sistema DEVE manter a mesma hierarquia de comandos e subcomandos da implementação atual, garantindo compatibilidade 100% com scripts e usuários existentes.

#### Cenário: Comando de nível superior funciona como antes
- **QUANDO** o usuário executa `fur <comando>` com os mesmos argumentos de antes da migração
- **ENTÃO** o comportamento e a saída DEVEM ser idênticos

#### Cenário: Subcomando de nível profundo funciona como antes
- **QUANDO** o usuário executa `fur database pgpass add <banco>` com os mesmos argumentos
- **ENTÃO** o comportamento e a saída DEVEM ser idênticos à implementação anterior

#### Cenário: Ajuda automática gerada pelo framework
- **QUANDO** o usuário executa `fur --help`, `fur <comando> --help` ou `fur <comando> <subcomando> --help`
- **ENTÃO** o CLI DEVE exibir descrições, argumentos e opções derivados dos atributos declarativos das classes de comando

---

### Requisito: Estrutura de arquivos reflete a hierarquia de comandos
O sistema DEVE organizar os arquivos de comando em subpastas que espelham a hierarquia do CLI.

#### Cenário: Navegação por grupo de comando
- **QUANDO** um desenvolvedor precisa localizar o código do comando `fur database backup`
- **ENTÃO** o arquivo DEVE estar em `FurLab.CLI/Commands/Database/Backup/DatabaseBackupCommand.cs`

#### Cenário: Um arquivo por comando folha
- **QUANDO** existe um comando folha (comando executável, sem subcomandos)
- **ENTÃO** DEVE existir exatamente um arquivo `.cs` correspondente contendo a classe de comando

---

### Requisito: Comandos sem DI permanecem funcionais como classes instanciáveis
O sistema DEVE converter comandos que não usam DI (ex: `file combine`, `windowsfeatures`, `winget`, `claude`, `opencode`) em classes instanciáveis sem quebrar sua lógica atual.

#### Cenário: Comando sem dependências de serviço
- **QUANDO** um comando não requer nenhum serviço do Core
- **ENTÃO** sua classe DEVE ter um construtor padrão (sem parâmetros) e implementar toda a lógica internamente

#### Cenário: Helpers estáticos continuam acessíveis
- **QUANDO** um comando usa `SecurityUtils`, `Utils` ou `SqlQueryAnalyzer`
- **ENTÃO** essas classes DEVEM permanecer estáticas e acessíveis diretamente — sem necessidade de injeção
