# DevMaid — Índice de Specs

Este documento é o índice mestre de todas as especificações de features do DevMaid. Cada spec segue a metodologia de [Desenvolvimento Orientado a Especificações](https://github.com/github/spec-kit) e é governada pela [Constituição do Projeto](../CONSTITUTION.md).

---

## Features Ativas

| ID | Slug | Nome | Status | Spec |
|----|------|------|--------|------|
| 001 | `table-parser` | Gerador de Classes a partir de Tabelas | Implementado | [spec.md](./001-table-parser/spec.md) |
| 002 | `database-utilities` | Utilitários de Banco de Dados (Backup e Restauração) | Implementado | [spec.md](./002-database-utilities/spec.md) |
| 003 | `file-combine` | Utilitários de Arquivo — Combinar | Implementado | [spec.md](./003-file-combine/spec.md) |
| 004 | `claude-code-integration` | Integração com Claude Code | Implementado | [spec.md](./004-claude-code-integration/spec.md) |
| 005 | `opencode-integration` | Integração com OpenCode | Implementado | [spec.md](./005-opencode-integration/spec.md) |
| 006 | `winget-package-manager` | Gerenciador de Pacotes Winget | Implementado | [spec.md](./006-winget-package-manager/spec.md) |
| 007 | `sql-query-csv-export` | Consulta SQL e Exportação CSV | Implementado | [spec.md](./007-sql-query-csv-export/spec.md) |
| 008 | `project-cleaner` | Limpador de Projetos (.NET Clean) | Implementado | [spec.md](./008-project-cleaner/spec.md) |
| 009 | `windows-features-manager` | Gerenciador de Recursos do Windows | Implementado | [spec.md](./009-windows-features-manager/spec.md) |

---

## Features Planejadas

| ID | Slug | Nome | Status | Spec |
|----|------|------|--------|------|
| 010 | `gui-electron-angular` | Interface Gráfica — Electron + Angular | Planejado (Rascunho) | [spec.md](./010-gui-electron-angular/spec.md) |

---

## Ciclo de Vida de uma Spec

```
Rascunho → Revisão → Aprovado → Implementado → Depreciado
```

| Estado | Significado |
|--------|------------|
| **Rascunho** | Em progresso; questões em aberto permanecem (`[NECESSITA ESCLARECIMENTO]`) |
| **Revisão** | Pronto para revisão; todas as questões resolvidas |
| **Aprovado** | Aceito; implementação pode começar |
| **Implementado** | Feature entregue e corresponde à spec |
| **Depreciado** | Feature removida ou substituída |

---

## Adicionando uma Nova Spec

1. Atribuir o próximo ID sequencial (ex.: `011`)
2. Criar `docs/specs/<ID>-<slug>/spec.md` usando a estrutura de template abaixo
3. Adicionar uma entrada a este índice
4. Marcar o status como **Rascunho** até que todos os marcadores `[NECESSITA ESCLARECIMENTO]` sejam resolvidos

### Estrutura do Template de Spec

```markdown
# Spec de Feature: <Nome>

**ID:** <NNN>
**Slug:** <slug>
**Status:** Rascunho | Revisão | Aprovado | Implementado | Depreciado
**Versão:** <semver>

## Propósito
## Histórias de Usuário
## Critérios de Aceitação
## Interface CLI
## Cenários de Erro
## Requisitos Não Funcionais
```
