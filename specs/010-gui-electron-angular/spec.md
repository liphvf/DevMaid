# Spec de Feature: Interface Gráfica — Electron + Angular

**ID:** 010  
**Slug:** gui-electron-angular  
**Status:** Planejado  
**Versão:** 0.1 — RASCUNHO  

---

## Propósito

Fornecer uma interface gráfica de desktop moderna para as capacidades do DevMaid, direcionada a desenvolvedores que preferem um fluxo de trabalho visual para tarefas como backup/restauração de banco de dados, execução de consultas e gerenciamento de ambiente — preservando paridade total com a CLI.

---

## Histórias de Usuário

**HU-010.1** — Como desenvolvedor que prefere ferramentas gráficas, quero realizar backups e restaurações de banco de dados por meio de um formulário visual, para não precisar lembrar flags e opções da CLI.

**HU-010.2** — Como desenvolvedor, quero ver feedback de progresso em tempo real durante operações longas (backup, restauração, execução de consulta) na GUI, para saber que a operação está avançando.

**HU-010.3** — Como desenvolvedor, quero gerenciar configurações de servidor (host, porta, credenciais) por meio de uma tela de configurações da GUI, para não precisar editar manualmente o `appsettings.json`.

**HU-010.4** — Como desenvolvedor, quero iniciar a GUI do DevMaid com um único comando (`devmaid gui`) pelo terminal, para alternar entre CLI e GUI de forma fluida.

**HU-010.5** — Como desenvolvedor, quero que todas as funcionalidades da GUI permaneçam acessíveis pela CLI, para que fluxos de automação e scripts nunca sejam interrompidos.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-010.1 | `devmaid gui` inicia o aplicativo desktop Electron e sai `0`. |
| CA-010.2 | A GUI expõe todas as features cobertas pela CLI: database, query, winget, file, claude, opencode, clean, windowsfeatures. |
| CA-010.3 | Operações longas exibem uma barra de progresso atualizada em tempo real via SignalR. |
| CA-010.4 | A tela de configurações lê e grava no `appsettings.json` usando o mesmo formato que a CLI. |
| CA-010.5 | A GUI se comunica com um backend `DevMaid.Api` REST + SignalR iniciado automaticamente quando a GUI é lançada. |
| CA-010.6 | Fechar a GUI também encerra o servidor API em segundo plano. |
| CA-010.7 | A GUI é distribuída como instalador Windows (NSIS). |

---

## Interface CLI

```bash
devmaid gui
```

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | GUI iniciada e fechada normalmente |
| `1` | Falha ao iniciar o servidor API |
| `1` | Binário da GUI não encontrado |

---

## Visão Geral da Arquitetura

```
Usuário
 └── devmaid gui (CLI)
      ├── Inicia DevMaid.Api (ASP.NET Core + SignalR) em segundo plano
      └── Lança DevMaid.Gui (Electron)
           └── App Angular (HTTP + SignalR) ──► DevMaid.Api
                                                 └── DevMaid.Core (lógica de negócio)
```

### Decisões Técnicas Principais

| Decisão | Escolha | Justificativa |
|---------|---------|---------------|
| Framework frontend | Angular 18+ | Tipagem estática, orientado a componentes, adequado para UIs com muitos formulários |
| Shell desktop | Electron | Multiplataforma, integra bem com tecnologias web |
| Transporte de API | REST + SignalR | REST para operações, SignalR para progresso em tempo real |
| Biblioteca de UI | Angular Material | Design consistente, acessível, bem mantida |

---

## Entrega em Fases

Esta feature é entregue em fases. Cada fase deve ser independentemente lançável.

| Fase | Escopo | Status |
|------|--------|--------|
| 1 | Extração da camada Core (`DevMaid.Core`) | Concluído — `DevMaid.Core` existe com `Interfaces/` (5 interfaces) e `Services/` (7 serviços) completos |
| 2 | Refatoração da CLI para usar a camada Core | Concluído — `DevMaid.CLI` usa `services.AddDevMaidServices()` do Core; classes em `DevMaid.CLI/Services/` são wrappers de compatibilidade que delegam para `DevMaid.Core` |
| 3 | Backend `DevMaid.Api` REST + SignalR | Planejado |
| 4 | Frontend Angular (todas as telas de features) | Planejado |
| 5 | Integração com Electron e empacotamento | Planejado |
| 6 | Comando híbrido `devmaid gui` na CLI | Planejado |

---

## Restrições

- A GUI é exclusiva para Windows no lançamento inicial (seguindo o escopo de plataforma da CLI).
- A GUI nunca deve expor funcionalidades que não sejam também acessíveis pela CLI.
- Nenhuma lógica de negócio nova pode ser introduzida na camada GUI — toda a lógica reside no `DevMaid.Core`.

---

## Questões em Aberto

- **API binding:** O servidor API deve escutar apenas em `localhost` (127.0.0.1) na fase inicial. Endereço de bind configurável pode ser introduzido em uma fase posterior mediante spec formal.
- **Autenticação da API:** O bind exclusivo em `localhost` é suficiente para a fase inicial. Autenticação baseada em token deve ser adicionada caso a API precise ser exposta fora do `localhost` no futuro. CORS deve ser configurado para aceitar apenas `app://*` (origem Electron).
- **Estado de configuração entre sessões:** A GUI deve lembrar a última configuração usada por operação (persistida em `appsettings.json`), alinhado com o comportamento da CLI (CA-010.4).
- **Versão mínima do Windows:** Windows 10 versão 1903 (build 18362) ou superior — requisito mínimo do Electron 28+ e compatível com o escopo de plataforma da CLI.

---

## Requisitos Não Funcionais

- A inicialização da GUI (de `devmaid gui` até a janela interativa) deve ser concluída em menos de **5 segundos** no hardware alvo.
- O aplicativo Angular deve atingir pontuação ≥ 90 na auditoria de acessibilidade do Lighthouse.
- O instalador empacotado deve ter menos de 200 MB.
