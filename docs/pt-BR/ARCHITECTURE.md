# Documentação de Arquitetura

## Visão Geral da Arquitetura

DevMaid é uma ferramenta de interface de linha de comando (CLI) baseada em .NET projetada usando uma arquitetura modular baseada em comandos. O aplicativo segue os princípios de separação de preocupações, com limites claros entre parsing de CLI, lógica de negócio e camadas de acesso a dados.

A arquitetura é construída sobre System.CommandLine para parsing de argumentos da CLI e Microsoft.Extensions.Configuration para gerenciamento flexível de configuração.

## Design de Alto Nível

```
┌─────────────────────────────────────────────────────────────────┐
│                        DevMaid CLI                              │
├─────────────────────────────────────────────────────────────────┤
│  Ponto de Entrada (Program.cs)                                  │
│  ├── Carregamento de Configuração                               │
│  ├── Registro de Comandos                                       │
│  └── Parsing de Argumentos                                     │
├─────────────────────────────────────────────────────────────────┤
│  Camada de Comandos (Commands/)                                 │
│  ├── TableParserCommand                                        │
│  ├── FileCommand                                               │
│  ├── ClaudeCodeCommand                                         │
│  ├── OpenCodeCommand                                           │
│  ├── WingetCommand                                             │
│  ├── QueryCommand                                              │
│  ├── CleanCommand                                              │
│  └── WindowsFeaturesCommand                                    │
├─────────────────────────────────────────────────────────────────┤
│  Camadas de Suporte                                            │
│  ├── CommandOptions (DTOs)                                     │
│  ├── Database (Npgsql)                                         │
│  └── Utils (Funções Auxiliares)                                │
└─────────────────────────────────────────────────────────────────┘
```

## Componentes Centrais e Responsabilidades

### 1. Program.cs (Ponto de Entrada)

**Responsabilidades:**
- Inicializar a configuração do aplicativo
- Registrar todos os comandos disponíveis
- Fazer parsing dos argumentos da linha de comando
- Invocar o manipulador de comando apropriado

**Métodos Principais:**
- `Main(string[] args)` - Ponto de entrada do aplicativo
- ConfigurationBuilder com JSON, variáveis de ambiente e user secrets

### 2. Camada de Comandos

Cada comando segue o padrão builder com um método estático `Build()` que retorna um objeto `Command`.

#### TableParserCommand
- Conecta ao banco de dados PostgreSQL usando Npgsql
- Recupera metadados da tabela
- Gera classe C# com propriedades baseadas nas definições de colunas

#### FileCommand (Combine)
- Combina múltiplos arquivos em um único
- Suporta padrões de arquivo com curingas
- Preserva codificação dos arquivos de origem

#### ClaudeCodeCommand
- Instala Claude Code via winget
- Configura configurações de banco de dados MCP
- Configura ambiente Windows para Claude

#### OpenCodeCommand
- Instala CLI do OpenCode
- Verifica status de instalação
- Gerenciamento de configuração

#### WingetCommand
- Exporta pacotes instalados para JSON
- Importa pacotes do backup
- Resolução de dependências entre pacotes

#### QueryCommand
- Executa consultas SQL e exporta os resultados para CSV
- Suporta múltiplos bancos de dados e servidores via configuração
- Integrado ao appsettings.json para servidores remotos

#### CleanCommand
- Exclui diretórios bin e obj do projeto ou solução selecionada
- Útil para debugar problemas de compilação ou economizar espaço
- Busca recursivamente os subdiretórios

#### WindowsFeaturesCommand
- Exporta as features opcionais do Windows atualmente ativadas em um JSON
- Importa features de um arquivo JSON (via dism.exe)
- Permite listar funcionalidades ativadas

### 3. Camada CommandOptions

Objetos de Transferência de Dados (DTOs) que representam opções da linha de comando:
- Classes de opções fortemente tipadas
- Atributos de validação
- Tratamento de valores padrão

## Fluxo de Dados

### CLI Execution Flow

### 1. Padrão Builder

Cada comando implementa um método estático `Build()` que constrói e configura o objeto de comando:

```csharp
public static Command Build()
{
    var command = new Command("winget", "Gerenciar pacotes winget.");
    // Adicionar opções e subcomandos
    return command;
}
```

### 2. Padrão Singleton (Configuração)

A propriedade `Program.AppSettings` fornece acesso centralizado à configuração do aplicativo.

### 3. Padrão Observador (Eventos de Processo)

Captura de saída em tempo real usa manipuladores de eventos:
- `OutputDataReceived`
- `ErrorDataReceived`

## Decisões Técnicas

### 1. System.CommandLine para Parsing de CLI

**Decisão:** Usar System.CommandLine em vez de parsing manual ou bibliotecas de terceiros.

**Justificativa:**
- Biblioteca .NET nativa
- Opções fortemente tipadas
- Geração de help integrada
- Suporte a subcomandos

### 2. Npgsql para Acesso a Banco de Dados

**Decisão:** Usar Npgsql como provedor PostgreSQL.

**Justificativa:**
- Driver .NET oficial do PostgreSQL
- Alto desempenho
- Suporte completo a recursos PostgreSQL
- Manutenção ativa

### 3. Microsoft.Extensions.Configuration

**Decisão:** Usar as extensões de configuração para configurações flexíveis.

**Justificativa:**
- Múltiplas fontes de configuração (JSON, variáveis de ambiente, user secrets)
- Tipagem forte com binding de configuração
- Padrão reconhecido pela indústria

## Considerações de Escalabilidade

### Extensibilidade de Comandos

A arquitetura suporta fácil adição de novos comandos:
1. Criar nova classe de comando em `Commands/`
2. Implementar o método `Build()`
3. Registrar em `Program.cs`

### Escalabilidade de Configuração

O sistema de configuração suporta:
- Arquivos de configuração específicos por ambiente
- Substituições por variáveis de ambiente
- User secrets para dados sensíveis

## Considerações de Segurança

### 1. Manipulação de Senhas

- Senhas podem ser fornecidas via linha de comando ou solicitadas com segurança
- Nenhum registro ou persistência de senha
- Suporte a user secrets para desenvolvimento

### 2. Execução de Processos

- Comandos são executados com os mesmos privilégios do usuário
- Sem execução de shell (UseShellExecute = false)
- Saída capturada e sanitizada

### 3. Segurança de Configuração

- Dados sensíveis armazenados em user secrets
- Variáveis de ambiente para implantação
- Sem credenciais hardcoded

## Melhorias Arquiteturais Futuras

### 1. Sistema de Plugins

Implementar uma arquitetura de plugins para permitir extensões de comando de terceiros:
- Assemblies separados para comandos
- Descoberta dinâmica de comandos
- Verificações de compatibilidade de versão

### 2. API de Configuração

Expor configuração via API para integração com outras ferramentas:
- Endpoint REST para consultas de configuração
- Recarregamento de configuração em tempo real

### 3. Framework de Relatório de Progresso

Criar um sistema unificado de relatório de progresso:
- UI de progresso consistente entre comandos
- Suporte a cancelamento
- Cálculos de tempo estimado

### 4. Framework de Logging

Adicionar logging estruturado:
- Logging baseado em arquivo
- Níveis de log
- Rotação de logs
- Integração com agregadores de log externos

### 5. Infraestrutura de Testes Unitários

Melhorar cobertura de testes:
- Testes unitários de comandos
- Testes de integração para operações de banco de dados

### 6. Suporte multiplataforma

Expansão além do Windows:
- Suporte a alternativas winget para macOS/Linux
- Implementações de comandos específicas por plataforma

## Estrutura de Diretórios

```
DevMaid/
├── DevMaid.CLI/               # Projeto principal (Linha de Comando)
│   ├── Program.cs             # Ponto de entrada
│   ├── Commands/              # Implementações de comandos (TableParser, Winget, Query, etc.)
│   ├── CommandOptions/        # Objetos DTOs de Options dos Comandos
│   └── Services/              # Logging, Utilitarios, Componentes da Aplicação, Listadores
├── DevMaid.Core/              # Bibliotecas Centrais e Utilitários de Domínio
│   ├── Interfaces/            # Contratos Injetáveis Compartilhados (ILogger, IFileService, etc.)
│   └── Services/              # Implementação de Executores (WingetService, DatabaseService, etc.)
├── DevMaid.Tests/             # Projeto de Bateria de Testes Unidade e Integração (MSTest)
└── docs/                      # Documentação
    ├── en/
    │   ├── ARCHITECTURE.md
    │   └── FEATURE_SPECIFICATION.md
    └── pt-BR/
        ├── ARCHITECTURE.md
        └── FEATURE_SPECIFICATION.md
```

## Conclusão

A arquitetura do DevMaid fornece uma base sólida para uma ferramenta CLI com:
- Separação clara de preocupações
- Extensibilidade fácil
- Código manutenível
- Gerenciamento de configuração flexível

O design modular permite fácil adição de novos recursos enquanto mantém qualidade de código e testabilidade.
