# DevMaid

Uma poderosa ferramenta CLI .NET para automatizar tarefas comuns de desenvolvimento.

## Descrição

DevMaid é uma interface de linha de comando (CLI) multiplataforma construída com .NET que ajuda desenvolvedores a automatizar tarefas repetitivas de desenvolvimento. Ela fornece comandos para operações de banco de dados, gerenciamento de arquivos, instalação de ferramentas de IA (Claude Code, OpenCode) e gerenciamento de pacotes Windows.

> **Nota**: Este é um projeto hobby criado para uso pessoal. Pode não seguir todas as melhores práticas ou ter testes abrangentes. Contribuições e feedback são bem-vindos, mas por favor tenha em mente que isso foi criado para resolver as necessidades específicas do autor.

## Problema que Resolve

Desenvolvedores frequentemente executam tarefas repetitivas que podem ser automatizadas:
- Converter esquemas de tabelas de banco de dados em classes C#
- Combinar múltiplos arquivos em um só
- Instalar e configurar ferramentas de IA para desenvolvimento
- Fazer backup e restaurar pacotes do Windows

DevMaid consolida essas tarefas em uma única ferramenta CLI fácil de usar.

## Principais Funcionalidades

- **Table Parser**: Analisa tabelas de banco de dados PostgreSQL e gera classes de propriedades C#
- **File Utils**: Pesquisa, organiza e encontra arquivos duplicados
- **Integração com Claude Code**: Instala e configura CLI do Claude Code
- **Integração com OpenCode**: Instala e configura CLI do OpenCode
- **Gerenciador Winget**: Faz backup e restaura pacotes do gerenciador de pacotes Windows
- **Modo TUI Interativo**: Interface de terminal amigável com navegação

## Tecnologias

- **Framework**: .NET 10
- **Linguagem**: C#
- **CLI Parsing**: System.CommandLine
- **TUI**: Terminal.Gui
- **Banco de Dados**: Npgsql (PostgreSQL)
- **Configuração**: Microsoft.Extensions.Configuration

## Instalação

### Pré-requisitos

- .NET SDK 10 ou superior
- Windows (necessário para comandos Claude, OpenCode e Winget)

### Instalar como Ferramenta .NET

```bash
dotnet tool install --global DevMaid
```

Ou instalar pelo NuGet:

```bash
dotnet tool install -g DevMaid
```

### Compilar a Partir do Código Fonte

```bash
git clone https://github.com/seu-repositorio/DevMaid.git
cd DevMaid
dotnet restore
dotnet build
```

## Como Executar Localmente

### Executar a Partir do Código Fonte

```bash
dotnet run -- --help
```

### Executar Modo TUI

```bash
devmaid tui
```

## Exemplos de Uso Básico

### Table Parser - Gerar Classe C# a Partir de Tabela de Banco de Dados

```bash
devmaid table-parser -d meubanco -t usuarios -u postgres -H localhost
```

### Combinar Arquivos

```bash
devmaid combine -i "C:\temp\*.sql" -o "C:\temp\resultado.sql"
```

### Instalar Claude Code

```bash
devmaid claude install
```

### Backup Winget

```bash
devmaid winget backup -o "C:\backup"
```

### Restaurar Winget

```bash
devmaid winget restore -i "C:\backup\backup-winget.json"
```

### Modo TUI Interativo

```bash
devmaid tui
```

Use as teclas de seta para navegar, Enter para selecionar, Esc para sair.

## Lista de Comandos

| Comando | Descrição |
|---------|-----------|
| `table-parser` | Analisa tabela de banco de dados e gera classe C# |
| `file` | Utilitários de gerenciamento de arquivos |
| `claude` | Integração com Claude Code |
| `opencode` | Integração com CLI do OpenCode |
| `winget` | Gerenciador de pacotes Windows |
| `tui` | Inicia modo TUI interativo |

## Documentação

Para informações mais detalhadas, consulte:

- [Arquitetura](./docs/pt-BR/ARCHITECTURE.md)
- [Especificação de Funcionalidades](./docs/pt-BR/FEATURE_SPECIFICATION.md)

## Contribuição

Contribuições são bem-vindas! Por favor, siga estes passos:

1. Fork o repositório
2. Crie uma branch de funcionalidade (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

Por favor, certifique-se de que todos os testes passam e que o código segue os padrões de codificação do projeto.

## Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](./LICENSE) para detalhes.

---

🇺🇸 English: [README.md](./README.md)  
🇧🇷 Português (padrão)
