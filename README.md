# DevMaid

DevMaid e uma CLI em .NET para automatizar tarefas comuns de desenvolvimento.

## O que o projeto faz

Hoje o DevMaid oferece estes comandos:

- `TableParser`: le colunas de uma tabela PostgreSQL e gera propriedades C#.
- `Combine`: combina varios arquivos em um unico arquivo de saida.
- `claude install`: instala o Claude Code via `winget`.
- `claude settings mcp-database`: executa o cadastro do MCP toolbox no Claude.
- `claude settings win-env`: atualiza `~/.claude.json` para usar `pwsh.exe` e liberar `edit/read/shell`.

## Requisitos

- .NET SDK 10
- Windows (obrigatorio para os comandos `claude`, pois usam `winget`/`pwsh`)
- Acesso a PostgreSQL para usar `TableParser`

## Como usar

### Rodando direto do codigo fonte

```bash
dotnet restore
dotnet build
dotnet run -- --help
```

### Como ferramenta instalada

Se voce instalar/publicar como .NET Tool, o comando sera `devmaid`.

```bash
devmaid --help
```

## Comandos

### 1) TableParser

Converte metadados de uma tabela em propriedades C#.

Exemplo:

```bash
devmaid TableParser -d meu_banco -t users -u postgres -H localhost -p minha_senha
```

Opcoes principais:

- `-d`, `--db` (obrigatorio): nome do banco.
- `-t`, `--table`: nome da tabela.
- `-u`, `--user`: usuario do banco (padrao: `postgres`).
- `-p`, `--password`: senha (se nao informar, o comando pede no terminal).
- `-H`, `--host`: host do banco (padrao: `localhost`).
- `-o`, `--output`: arquivo de saida.

Obs.: na implementacao atual, o arquivo gerado e salvo em `./tabela.class`.

### 2) Combine

Combina varios arquivos em um arquivo unico.

Exemplo:

```bash
devmaid Combine -i "C:\\tmp\\*.sql" -o "C:\\tmp\\resultado.sql"
```

Opcoes:

- `-i`, `--input` (obrigatorio): padrao de arquivos de entrada.
- `-o`, `--output`: arquivo final. Se nao informar, o comando gera um arquivo `CombineFiles.<ext>` no mesmo diretorio.

### 3) Claude

#### `claude install`

Instala o Claude Code com:

```bash
winget install --id Anthropic.ClaudeCode -e --accept-package-agreements --accept-source-agreements
```

#### `claude settings mcp-database`

Executa exatamente:

```bash
claude mcp add --transport sse toolbox http://127.0.0.1:5000/mcp/sse --scope user
```

#### `claude settings win-env`

Atualiza `%USERPROFILE%\\.claude.json` com:

```json
{
  "shell": "pwsh.exe",
  "permission": {
    "edit": "allow",
    "read": "allow",
    "shell": "allow"
  }
}
```

## Estrutura do projeto

- `Program.cs`: inicializacao e registro dos comandos raiz.
- `CliCommands/`: definicao da arvore de comandos da CLI.
- `Commands/`: logica de negocio de cada comando.
- `CommandOptions/`: DTOs de opcoes usadas pelos comandos.

## Contribuicao

Contribuicoes sao bem-vindas. Abra uma issue ou envie um PR.

[Nuget Tool](https://www.nuget.org/packages/devmaid/)
