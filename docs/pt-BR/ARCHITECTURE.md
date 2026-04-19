# Documentação de Arquitetura

## Visão Geral da Arquitetura

FurLab é uma ferramenta de interface de linha de comando (CLI) baseada em .NET projetada usando uma arquitetura modular baseada em comandos. O aplicativo segue os princípios de separação de preocupações, com limites claros entre parsing de CLI, lógica de negócio e camadas de acesso a dados.

A arquitetura é construída sobre **Spectre.Console.Cli** para parsing de argumentos da CLI e uma UI rica, e **Microsoft.Extensions.DependencyInjection** para gerenciamento de dependências.

## Design de Alto Nível

```
┌─────────────────────────────────────────────────────────────────┐
│                        FurLab CLI                              │
├─────────────────────────────────────────────────────────────────┤
│  Ponto de Entrada (Program.cs)                                  │
│  ├── Configuração do Host (DI)                                  │
│  ├── Registro de Comandos (CommandApp)                          │
│  └── Tratamento Global de Exceções                              │
├─────────────────────────────────────────────────────────────────┤
│  Camada de Comandos (Commands/)                                 │
│  ├── File/ (FileCombineCommand)                                │
│  ├── Claude/ (Install, Settings)                                │
│  ├── OpenCode/ (Settings)                                       │
│  ├── Winget/ (Backup, Restore)                                  │
│  ├── Database/ (Backup, Restore, PgPass)                        │
│  ├── Docker/ (Postgres)                                         │
│  ├── Query/ (QueryRunCommand)                                   │
│  ├── WindowsFeatures/ (Export, Import, List)                    │
│  └── Settings/ (DbServers)                                      │
├─────────────────────────────────────────────────────────────────┤
│  Infraestrutura (Infrastructure/)                               │
│  ├── TypeRegistrar (Adapter para Spectre.Console.Cli)           │
│  └── TypeResolver                                               │
└─────────────────────────────────────────────────────────────────┘
```

## Componentes Centrais e Responsabilidades

### 1. Program.cs (Ponto de Entrada)

**Responsabilidades:**
- Configurar o container de Injeção de Dependência (DI)
- Configurar a aplicação `CommandApp` do Spectre.Console
- Definir a hierarquia de comandos e subcomandos (branches)
- Implementar o mapeamento global de exceções para códigos de saída (exit codes)

### 2. Camada de Comandos

Cada comando é uma classe que herda de `Command<TSettings>` ou `AsyncCommand<TSettings>`.

- **Settings**: Classe aninhada `public sealed class Settings : CommandSettings` que define os argumentos e opções do comando usando atributos como `[CommandArgument]` e `[CommandOption]`.
- **Injeção de Dependência**: Os comandos recebem serviços via construtor.
- **Execução**: A lógica do comando reside no método `Execute` ou `ExecuteAsync`, que deve apenas validar os inputs (via Settings) e delegar a execução para os serviços apropriados no `FurLab.Core`.

### 3. Camada de Infraestrutura

Fornece a ponte entre o container de DI do .NET (`IServiceCollection`) e o Spectre.Console.Cli através das classes `TypeRegistrar` e `TypeResolver`.

## Decisões Técnicas

### 1. Spectre.Console.Cli para Parsing de CLI

**Decisão:** Substituir `System.CommandLine` pelo `Spectre.Console.Cli`.

**Justificativa:**
- Melhor suporte para interfaces TUI (Tabelas, Progresso, Status, Prompts interativos)
- Integração nativa com injeção de dependência
- Definição de comandos baseada em classes, facilitando a manutenção e testes
- Formatação ANSI automática e simplificada

### 2. Injeção de Dependência Nativa

**Decisão:** Usar `Microsoft.Extensions.DependencyInjection`.

**Justificativa:**
- Padrão oficial do .NET
- Facilita o desacoplamento entre CLI e lógica de negócio
- Permite a substituição de serviços por Mocks em testes unitários

### 3. Armazenamento Seguro de Credenciais

**Decisão:** Implementar `ICredentialService` para gerenciar senhas de banco de dados.

**Justificativa:**
- Evita o armazenamento de senhas em texto plano no `appsettings.json`
- Protege dados sensíveis do usuário através de criptografia (Windows Data Protection API ou similar)

## Considerações de Segurança

### 1. Validação de Input

- Uso rigoroso de `SecurityUtils` para validar caminhos de arquivo, identificadores de banco de dados, hosts e portas.
- Prevenção contra Path Traversal e Injeção de comandos SQL através de sanitização de strings.

### 2. Manipulação de Senhas

- Senhas nunca são logadas ou exibidas em texto plano.
- Prompts interativos mascaram a entrada do usuário.
- Uso de `pgpass.conf` e `ICredentialService` para evitar a passagem de senhas via argumentos de linha de comando (que ficam visíveis no histórico do shell).

## Estrutura de Diretórios

```
FurLab/
├── FurLab.CLI/               # Projeto de Interface (Spectre.Console)
│   ├── Program.cs             # Configuração e Registro de Comandos
│   ├── Commands/              # Classes de Comando (Organizadas por subdiretório)
│   ├── Infrastructure/        # Adaptadores de DI para o CLI
│   └── SecurityUtils.cs       # Validadores de Segurança
├── FurLab.Core/              # Lógica de Negócio e Contratos
│   ├── Interfaces/            # Interfaces de Serviço
│   ├── Services/              # Implementações de Serviço
│   ├── Models/                # DTOs e Modelos de Dados
│   └── Logging/               # Abstração de Logging
├── FurLab.Tests/             # Testes MSTest + Moq
└── docs/                      # Documentação (pt-BR e en)
```
