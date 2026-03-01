# Documentação de Arquitetura

## Visão Geral da Arquitetura

DevMaid é uma ferramenta de interface de linha de comando (CLI) baseada em .NET projetada usando uma arquitetura modular baseada em comandos. O aplicativo segue os princípios de separação de preocupações, com limites claros entre parsing de CLI, lógica de negócio e camadas de acesso a dados.

A arquitetura é construída sobre System.CommandLine para parsing de argumentos da CLI, Terminal.Gui para o modo TUI interativo, e Microsoft.Extensions.Configuration para gerenciamento flexível de configuração.

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
│  └── TuiCommand                                                │
├─────────────────────────────────────────────────────────────────┤
│  Camada TUI (Tui/)                                             │
│  ├── TuiApp (Aplicação Principal)                              │
│  └── MenuItem (Modelo de Dados)                                │
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

#### FileCommand
- Funcionalidade de busca de arquivos
- Organização por extensão
- Detecção de arquivos duplicados

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

#### TuiCommand
- Inicia a interface de terminal interativa
- Ponto de entrada para TuiApp

### 3. Camada TUI (Tui/)

#### TuiApp
- Controlador principal da aplicação TUI
- Gerencia navegação e renderização de menus
- Gerencia execução de comandos em tempo real com exibição de progresso
- Detecta tema do terminal (claro/escuro)

#### MenuItem
- Modelo de dados para entradas de menu
- Contém propriedades Nome, Descrição e Ação

### 4. Camada CommandOptions

Objetos de Transferência de Dados (DTOs) que representam opções da linha de comando:
- Classes de opções fortemente tipadas
- Atributos de validação
- Tratamento de valores padrão

## Fluxo de Dados

### Fluxo de Execução CLI

```
Entrada do Usuário
    ↓
System.CommandLine Parser
    ↓
Manipulador de Comando (ex: WingetCommand.RunBackup)
    ↓
Lógica de Negócio
    ↓
Serviço Externo (Winget, PostgreSQL, Sistema de Arquivos)
    ↓
Saída/Resultado
```

### Fluxo de Execução TUI

```
Usuário Lança TUI
    ↓
TuiApp.Run()
    ↓
Detectar Tema do Terminal
    ↓
Renderizar Menu Principal
    ↓
Navegação do Usuário (Teclas de Seta)
    ↓
Seleção → Executar Ação
    ↓
Mostrar Diálogo de Progresso/Saída
    ↓
Retornar ao Menu / Sair
```

### Fluxo de Execução de Comando em Tempo Real

```
Usuário Seleciona Comando
    ↓
Mostrar Diálogo de Progresso
    ↓
Iniciar Processo (assíncrono)
    ↓
Capturar Saída (OutputDataReceived)
    ↓
Atualizar UI (Application.MainLoop.Invoke)
    ↓
Esperar Conclusão
    ↓
Mostrar Código de Saída
    ↓
Permitir Usuário Fechar
```

## Padrões de Projeto Utilizados

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

### 3. Padrão Estratégia (Tema TUI)

O método `DetectTerminalTheme()` determina o esquema de cores apropriado com base na detecção do terminal, com estratégias separadas para temas claros e escuros.

### 4. Padrão Comando (MenuItems)

Cada item de menu encapsula uma ação que pode ser executada:

```csharp
new MenuItem("Nome", "Descrição", () => ExecutarComando("..."))
```

### 5. Padrão Observador (Eventos de Processo)

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

### 2. Terminal.Gui para TUI

**Decisão:** Usar Terminal.Gui para a interface de terminal interativa.

**Justificativa:**
- Madura e bem mantida
- Suporte multiplataforma
- API declarativa
- Boa documentação

### 3. Npgsql para Acesso a Banco de Dados

**Decisão:** Usar Npgsql como provedor PostgreSQL.

**Justificativa:**
- Driver .NET oficial do PostgreSQL
- Alto desempenho
- Suporte completo a recursos PostgreSQL
- Manutenção ativa

### 4. Microsoft.Extensions.Configuration

**Decisão:** Usar as extensões de configuração para configurações flexíveis.

**Justificativa:**
- Múltiplas fontes de configuração (JSON, variáveis de ambiente, user secrets)
- Tipagem forte com binding de configuração
- Padrão reconhecido pela indústria

### 5. Execução de Processo Assíncrono no TUI

**Decisão:** Executar comandos externos de forma assíncrona com atualizações de UI em tempo real.

**Justificativa:**
- UI não-bloqueante
- Exibição de saída em tempo real
- Melhor experiência do usuário para comandos de longa duração

## Considerações de Escalabilidade

### Extensibilidade de Comandos

A arquitetura suporta fácil adição de novos comandos:
1. Criar nova classe de comando em `Commands/`
2. Implementar o método `Build()`
3. Registrar em `Program.cs`

### Extensibilidade do Menu TUI

Adicionar novos itens de menu é simples:
1. Criar MenuItem com nome, descrição e ação
2. Adicionar à lista de menu apropriada

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
- Testes de interação com TUI

### 6. Suporte multiplataforma

Expansão além do Windows:
- Suporte a alternativas winget para macOS/Linux
- Implementações de comandos específicas por plataforma

## Estrutura de Diretórios

```
DevMaid/
├── Program.cs                 # Ponto de entrada
├── DevMaid.csproj            # Arquivo do projeto
├── Commands/                  # Implementações de comandos
│   ├── TuiCommand.cs
│   ├── TableParserCommand.cs
│   ├── FileCommand.cs
│   ├── ClaudeCodeCommand.cs
│   ├── OpenCodeCommand.cs
│   └── WingetCommand.cs
├── CommandOptions/            # DTOs para comandos
├── Tui/                       # Componentes TUI
│   ├── TuiApp.cs
│   └── MenuItem.cs
├── Utils.cs                   # Funções auxiliares
├── Database.cs               # Utilitários de banco de dados
└── docs/                     # Documentação
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
- Boa experiência do usuário através do modo TUI
- Gerenciamento de configuração flexível

O design modular permite fácil adição de novos recursos enquanto mantém qualidade de código e testabilidade.
