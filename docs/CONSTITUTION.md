# DevMaid — Constituição do Projeto

> **Versão:** 1.0  
> **Data:** Abril de 2026  
> **Status:** Ativa  
> **Autor:** Filiphe Vilar Figueiredo

---

## Preâmbulo

DevMaid é uma ferramenta CLI baseada em .NET com o propósito de automatizar e simplificar tarefas recorrentes de desenvolvimento. Esta constituição estabelece os princípios governantes e as diretrizes de desenvolvimento que toda feature, decisão arquitetural e contribuição devem respeitar. É a única fonte de verdade sobre *como* o projeto é construído — independentemente de qual tecnologia, versão de framework ou membro de equipe esteja envolvido.

As especificações definem o **o quê** e o **por quê**. Esta constituição governa o **como**.

---

## Artigo I — Propósito e Escopo

### I.1 Missão

DevMaid existe para eliminar o atrito repetitivo nos fluxos de trabalho de desenvolvedores — especialmente em torno de bancos de dados, operações de arquivos, gerenciamento de pacotes Windows e configuração de ferramentas de IA — por meio de uma interface CLI unificada e composável.

### I.2 Usuário-Alvo

O usuário primário é um desenvolvedor Windows que:
- Trabalha diariamente com bancos de dados PostgreSQL
- Gerencia ambientes e pacotes Windows
- Utiliza assistentes de codificação com IA (Claude Code, OpenCode)
- Valoriza produtividade e automação em vez de passos manuais

### I.3 Fora do Escopo

Os itens a seguir estão explicitamente fora do escopo, a menos que uma spec formal seja aprovada:
- Features exclusivamente web sem equivalente CLI
- Funcionalidades com foco primário em não-Windows no curto prazo
- Lógica de negócio que pertence a uma ferramenta de domínio separada

---

## Artigo II — Princípios de Arquitetura

### II.1 CLI em Primeiro Lugar

A interface primária do DevMaid é a CLI. Toda feature **deve** ser completamente operável pela linha de comando. Interfaces gráficas (quando existirem) são projeções secundárias da funcionalidade acessível via CLI.

### II.2 Arquitetura Modular de Comandos

Cada capacidade **deve** ser implementada como um módulo de comando isolado:

```
Commands/
├── <Feature>Command.cs     ← binding da CLI
├── CommandOptions/
│   └── <Feature>Options.cs ← DTOs fortemente tipados
└── Services/
    └── <Feature>Service.cs ← lógica de negócio
```

A lógica de negócio nunca deve residir dentro da classe `Command`. Comandos são wrappers finos que analisam a entrada, delegam para serviços e reportam a saída.

### II.3 Estrutura de Projetos em Camadas

Os projetos são organizados da seguinte forma (em ordem de dependência):

| Projeto | Responsabilidade |
|---------|-----------------|
| `DevMaid.Core` | Toda lógica de negócio, serviços, interfaces, modelos |
| `DevMaid.CLI` | Parsing CLI, definições de comandos, formatação de saída |
| `DevMaid.Api` | API REST/SignalR (ponte futura para GUI) |
| `DevMaid.Gui` | GUI Electron + Angular (futuro) |
| `DevMaid.Tests` | Todos os projetos de teste |

> **Regra:** Projetos de nível superior podem depender de projetos de nível inferior. O inverso é proibido.

### II.4 Design Orientado a Interfaces

Todo serviço **deve** ser definido por uma interface em `DevMaid.Core/Interfaces/`. Implementações concretas ficam em `DevMaid.Core/Services/`. Isso permite testes sem infraestrutura e substituição futura.

### II.5 Sem Execução via Shell

Processos invocados pelo DevMaid **devem** usar `UseShellExecute = false`. A saída deve ser capturada via `RedirectStandardOutput` e `RedirectStandardError`. Nenhum comando deve invocar `cmd.exe` ou `powershell.exe` como shell intermediário.

---

## Artigo III — Padrões de Design de Comandos

### III.1 Nomenclatura de Comandos

Os comandos usam substantivos e verbos em `kebab-case` seguindo este padrão:

```
devmaid <substantivo> <verbo> [argumentos] [opções]
devmaid <substantivo> [opções]        ← para comandos de ação única
```

Exemplos:
- `devmaid database backup`
- `devmaid query run`
- `devmaid winget restore`
- `devmaid clean`

### III.2 Convenções de Opções

| Convenção | Regra |
|-----------|-------|
| Flags curtas | Caractere único, `-x` |
| Flags longas | Palavra completa, `--nome-da-opcao` |
| Opções obrigatórias | Devem ser documentadas; usar argumento posicional quando for mais claro |
| Opções de senha | Nunca obrigatórias; solicitar interativamente se não fornecidas |
| Caminhos de saída | Padrão para o diretório atual ou convenção sensata quando omitidos |

### III.3 Códigos de Saída

| Código | Significado |
|--------|-------------|
| `0` | Sucesso |
| `1` | Erro geral |
| `2` | Argumentos inválidos / opção obrigatória ausente |
| `3` | Dependência externa não encontrada (ex.: psql, pg_dump) |

### III.4 Padrões de Saída

- Feedback de progresso é escrito em **stdout** durante operações longas
- Erros são escritos em **stderr**
- Saídas CSV/arquivo vão para o caminho especificado por `--output`
- Nenhum código de cor ANSI sem verificação de detecção de terminal

---

## Artigo IV — Gerenciamento de Configuração

### IV.1 Fontes de Configuração (Precedência, da maior para a menor)

1. Opções da linha de comando
2. Variáveis de ambiente
3. `appsettings.<ambiente>.json`
4. `appsettings.json`
5. User secrets (apenas em desenvolvimento)

### IV.2 Dados Sensíveis

- Senhas **nunca** são armazenadas em `appsettings.json` commitado no controle de versão
- Senhas devem ser solicitadas interativamente quando não fornecidas via flag CLI
- User secrets são aceitáveis para desenvolvimento local
- Para ambientes CI/CD e produção, variáveis de ambiente são obrigatórias

### IV.3 Estrutura do appsettings.json

O arquivo de configuração deve seguir o schema estabelecido. Novas seções de configuração requerem uma revisão documentada com `[NECESSITA ESCLARECIMENTO]` antes da implementação.

---

## Artigo V — Tratamento de Erros

### V.1 Erros Devem Ser Acionáveis

Toda mensagem de erro exibida ao usuário deve:
1. Indicar **o que** falhou (recurso, operação ou parâmetro específico)
2. Sugerir **o que** o usuário deve fazer para resolver

Ruim: `"Erro: falha na conexão"`  
Bom: `"Conexão com postgres@localhost:5432 falhou. Verifique se o PostgreSQL está em execução e se as credenciais estão corretas."`

### V.2 Sem Falhas Silenciosas

Operações que têm sucesso parcial devem reportar tanto as partes bem-sucedidas quanto as que falharam. Nunca retornar código de saída `0` quando qualquer parte do trabalho solicitado falhou.

### V.3 Erros de Dependência Externa

Quando um binário externo (ex.: `psql`, `pg_dump`, `winget`) não for encontrado, o erro deve:
- Nomear o binário ausente
- Fornecer instruções de instalação ou um link para elas
- Sair com código `3`

---

## Artigo VI — Testes

### VI.1 Test-First na Lógica de Negócio

A lógica de negócio em `DevMaid.Core` deve ter testes unitários escritos **antes** ou **junto com** a implementação. Nenhum método de serviço pode ser mesclado na main sem pelo menos um teste unitário passando que cubra o caminho de sucesso principal.

### VI.2 Testes de Integração para Dependências Externas

Operações que tocam o sistema de arquivos, PostgreSQL ou processos externos devem ter testes de integração que rodem contra infraestrutura real (ou containerizada). Mocks são aceitáveis em testes unitários, mas não devem substituir testes de integração.

### VI.3 Estrutura do Projeto de Testes

```
DevMaid.Tests/
├── Core/           ← Testes unitários dos serviços Core
├── CLI/            ← Testes unitários do parsing de comandos
└── Integration/    ← Testes de integração (banco, sistema de arquivos, processos)
```

### VI.4 Convenção de Nomenclatura de Testes

```
<NomeDoMetodo>_<EstadoEmTeste>_<ComportamentoEsperado>
BackupAsync_ComOpcoesValidas_DeveCriarArquivoDump
BackupAsync_ComHostInvalido_DeveLancarExcecaoDeConexao
```

---

## Artigo VII — Portão de Simplicidade

### VII.1 Sem Arquitetura Especulativa

Features não devem introduzir abstrações, camadas ou padrões para casos de uso hipotéticos futuros. Toda decisão de design deve ser justificada por um requisito atual e documentado.

### VII.2 Orçamento Máximo de Complexidade

Novas features podem introduzir no máximo **um novo projeto** na solução. A introdução de projetos adicionais requer aprovação explícita e justificativa documentada na spec da feature.

### VII.3 Disciplina de Dependências

Antes de adicionar um novo pacote NuGet:
1. Verificar se nenhum pacote existente já cobre a necessidade
2. Verificar se está sendo mantido ativamente (último lançamento < 18 meses)
3. Documentar a justificativa na seção "Decisões Técnicas" da spec da feature

---

## Artigo VIII — Preocupações Transversais

### VIII.1 Logging

Todas as operações **devem** usar a interface `ILogger` do `DevMaid.Core`. A saída no console para o usuário é distinta do logging estruturado para diagnósticos.

### VIII.2 Reporte de Progresso

Operações de longa duração (> 2 segundos esperado) **devem** emitir progresso incremental via `IProgress<OperationProgress>`. Comandos exibem esse progresso no terminal. Camadas futuras de API/GUI consomem isso via SignalR.

### VIII.3 Cancelamento

Todas as operações assíncronas **devem** aceitar e respeitar `CancellationToken`. Comandos CLI devem vincular o `CancellationToken` ao `Ctrl+C`.

### VIII.4 Segurança

- Caminhos de entrada devem ser validados contra ataques de path traversal usando `SecurityUtils.IsValidPath()`
- Identificadores PostgreSQL devem ser validados antes da interpolação em qualquer query
- Senhas nunca devem aparecer em logs, stack traces ou textos de ajuda da CLI

---

## Artigo IX — Documentação

### IX.1 Localização dos Documentos

| Documento | Localização Primária (pt-BR) | Localização Secundária (en) |
|-----------|-----------------------------|-----------------------------|
| Arquitetura | `docs/pt-BR/ARCHITECTURE.md` | `docs/en/ARCHITECTURE.md` |
| Especificações de Features | `docs/specs/<NNN>-<slug>/spec.md` | — |
| Planos de Implementação | `docs/specs/<NNN>-<slug>/plan.md` | — |
| Referência de Comandos | `docs/pt-BR/FEATURE_SPECIFICATION.md` | `docs/en/FEATURE_SPECIFICATION.md` |
| Esta Constituição | `docs/CONSTITUTION.md` | — |

Em caso de divergência entre versões pt-BR e en, a versão **pt-BR é a fonte de verdade**.

### IX.2 Requisito de Spec de Feature

Toda nova feature **deve** ter um arquivo spec em `docs/specs/` antes que qualquer implementação comece. A spec define:
- Propósito e histórias de usuário
- Critérios de aceitação
- Cenários de erro
- Contrato da interface CLI (opções, códigos de saída)

### IX.3 Idioma da Documentação

- Documentação principal: **Português (pt-BR)**
- Specs e constituição: **Português (pt-BR)**

---

## Artigo X — Processo de Emenda

Esta constituição pode ser emendada quando:
1. Um problema recorrente de implementação revela um princípio mal especificado
2. Uma nova direção arquitetural (ex.: GUI, multiplataforma) requer novas regras
3. A maioria dos contribuidores ativos concorda que a mudança melhora qualidade ou clareza

As emendas devem:
- Declarar a **justificativa** para a mudança
- Atualizar a **versão** e a **data** deste documento
- Ser revisadas em um pull request antes de mesclar

---

## Glossário

| Termo | Definição |
|-------|-----------|
| CLI | Interface de Linha de Comando (Command-Line Interface) |
| DTO | Objeto de Transferência de Dados (Data Transfer Object) |
| MCP | Protocolo de Contexto de Modelo (Model Context Protocol) |
| SDD | Desenvolvimento Orientado a Especificações (Spec-Driven Development) |
| Winget | Gerenciador de Pacotes do Windows |
| Spec de Feature | Um arquivo `spec.md` em `docs/specs/` descrevendo uma única feature |
| Constituição | Este documento — os princípios governantes do DevMaid |
