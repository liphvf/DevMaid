---
description: Convert existing tasks into actionable, dependency-ordered GitHub issues for the feature based on available design artifacts.
tools: ['github/github-mcp-server/issue_write']
---

> **AVISO:** Todo output para o usuário deve ser em **português do Brasil (pt-BR)**. Isso inclui explicações, resumos, perguntas, mensagens de erro e qualquer comunicação direta. Exceção: não traduza nomes de skills, comandos (ex: `openspec-propose`, `opsx-apply`), identificadores de código, nomes de arquivos ou comandos de terminal.

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).
> **IDIOMA OBRIGATÓRIO — NÃO-NEGOCIÁVEL**: A constituição do projeto determina **pt-BR (Português do Brasil)** como idioma canônico para toda documentação gerada. Todo artefato produzido por este comando (arquivos .md, relatórios, checklists, seções adicionadas a specs, planos ou tarefas) **DEVE ser escrito em pt-BR**, independentemente do idioma usado pelo usuário na descrição da feature ou nos argumentos do comando.

## Outline

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").
1. From the executed script, extract the path to **tasks**.
1. Get the Git remote by running:

```bash
git config --get remote.origin.url
```

> [!CAUTION]
> ONLY PROCEED TO NEXT STEPS IF THE REMOTE IS A GITHUB URL

1. For each task in the list, use the GitHub MCP server to create a new issue in the repository that is representative of the Git remote.

> [!CAUTION]
> UNDER NO CIRCUMSTANCES EVER CREATE ISSUES IN REPOSITORIES THAT DO NOT MATCH THE REMOTE URL
