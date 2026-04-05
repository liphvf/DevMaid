# Tasks: Comando CLI para Configurar pgpass

**Input**: Documentos de design em `specs/011-pgpass-cli-setup/`
**Prerequisites**: `plan.md`, `spec.md`, `data-model.md`, `research.md`, `contracts/pgpass-command.md`

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: A qual user story pertence (US1, US2, US3)
- Caminhos de arquivo exatos incluídos nas descrições

---

## Phase 1: Setup (Infraestrutura Compartilhada)

**Objetivo**: Modificações mínimas nos arquivos existentes para registrar o novo serviço e ancorar o comando na hierarquia CLI.

- [X] T001 Registrar `IPgPassService` → `PgPassService` como singleton em `DevMaid.Core/Services/ServiceCollectionExtensions.cs`
- [X] T002 Adicionar `PgPassCommand.Build()` como subcomando em `DevMaid.CLI/Commands/DatabaseCommand.cs`

---

## Phase 2: Fundação (Pré-requisitos Bloqueantes)

**Objetivo**: Criar os artefatos compartilhados que todas as user stories dependem.

**CRÍTICO**: Nenhuma user story pode começar enquanto esta fase não estiver completa.

- [X] T003 [P] Criar `DevMaid.Core/Models/PgPassEntry.cs` — record com campos Hostname, Port, Database, Username, Password e IdentityKey
- [X] T004 [P] Criar `DevMaid.Core/Models/PgPassResult.cs` — record com Success, Message, IsDuplicate e factory methods Ok/Duplicate/Fail
- [X] T005 [P] Criar `DevMaid.Core/Interfaces/IPgPassService.cs` — interface com assinaturas de AddEntry, ListEntries e RemoveEntry
- [X] T006 [P] Criar `DevMaid.CLI/CommandOptions/PgPassCommandOptions.cs` — DTOs PgPassAddOptions, PgPassListOptions e PgPassRemoveOptions

**Checkpoint**: Fundação pronta — implementação das user stories pode começar.

---

## Phase 3: User Story 1 — Configurar Arquivo de Senhas PostgreSQL via CLI (Prioridade: P1) MVP

**Objetivo**: Usuário consegue executar `devmaid database pgpass add <banco>` e o `pgpass.conf` é criado/atualizado corretamente.

**Teste Independente**: Executar `devmaid database pgpass add meu_banco --password senha123` e verificar que `%APPDATA%\postgresql\pgpass.conf` contém `localhost:5432:meu_banco:postgres:senha123`.

### Testes para User Story 1

> **IMPORTANTE: Escrever estes testes PRIMEIRO — garantir que FALHAM antes da implementação**

- [X] T007 [P] [US1] Criar `DevMaid.Tests/Commands/PgPassServiceTests.cs` com testes para:
  - `AddEntry` cria diretório se não existir
  - `AddEntry` escreve entrada no formato correto
  - `AddEntry` preserva entradas existentes ao adicionar nova
  - `AddEntry` detecta duplicata e retorna `PgPassResult.Duplicate`
  - `AddEntry` escapa `:` → `\:` e `\` → `\\` na senha
  - `AddEntry` falha com senha vazia (retorna `PgPassResult.Fail`)
  - `AddEntry` aplica padrões: hostname=localhost, port=5432, username=postgres
  - `AddEntry` usa `*` como banco quando banco for omitido/`*`
- [X] T008 [P] [US1] Criar `DevMaid.Tests/Commands/PgPassCommandTests.cs` com testes para:
  - Subcomando `add` com banco obrigatório ausente retorna código de saída `2`
  - Subcomando `add` com host inválido retorna código de saída `2`
  - Subcomando `add` com porta inválida retorna código de saída `2`

### Implementação para User Story 1

- [X] T009 [US1] Criar `DevMaid.Core/Services/PgPassService.cs` — implementar `IPgPassService` com:
  - `PgPassResult AddEntry(PgPassEntry entry, string filePath)` — validação, escape, criação de diretório, detecção de duplicata, append ao arquivo
  - Método privado `EscapePassword(string password)` — escape de `:` e `\`
  - Método privado `ParseLine(string line)` — desserialização de linha pgpass para `PgPassEntry`
  - Método privado `SerializeEntry(PgPassEntry entry)` — serialização para formato pgpass
  - Método privado `ResolvePath()` — resolve `%APPDATA%\postgresql\pgpass.conf`
  - Tratamento de `UnauthorizedAccessException` → `PgPassResult.Fail` com mensagem RF-012
  - Tratamento de `IOException` → `PgPassResult.Fail` com mensagem RF-013
- [X] T010 [US1] Criar `DevMaid.CLI/Commands/PgPassCommand.cs` — implementar `Build()` retornando `Command("pgpass")` com subcomando `add`:
  - Argumento posicional `<banco>` (obrigatório)
  - Opção `--password` / `-W` (opcional; prompt interativo via `PostgresPasswordHandler` se ausente)
  - Opção `--host` / `-h` (padrão: `localhost`)
  - Opção `--port` / `-p` (padrão: `5432`)
  - Opção `--username` / `-U` (padrão: `postgres`)
  - Exibir mensagem de sucesso/erro conforme contrato (`contracts/pgpass-command.md`)
  - Retornar código de saída correto: `0` sucesso/duplicata, `1` erro I/O, `2` arg inválido

**Checkpoint**: US1 deve ser completamente funcional e testável de forma independente.

---

## Phase 4: User Story 2 — Listar Entradas Existentes do pgpass (Prioridade: P2)

**Objetivo**: Usuário consegue executar `devmaid database pgpass list` e ver todas as entradas com senhas mascaradas.

**Teste Independente**: Adicionar duas entradas via `pgpass add`, executar `pgpass list` e verificar que a saída tabular mostra ambas as entradas com `****` no campo senha.

### Testes para User Story 2

> **IMPORTANTE: Escrever estes testes PRIMEIRO — garantir que FALHAM antes da implementação**

- [X] T011 [P] [US2] Adicionar testes em `DevMaid.Tests/Commands/PgPassServiceTests.cs` para:
  - `ListEntries` retorna lista vazia quando arquivo não existe
  - `ListEntries` retorna lista vazia quando arquivo está vazio
  - `ListEntries` ignora linhas de comentário (`#`)
  - `ListEntries` retorna todas as entradas corretamente parseadas
  - `ListEntries` aplica unescape (`\:` → `:` e `\\` → `\`) na senha ao ler
- [X] T012 [P] [US2] Adicionar testes em `DevMaid.Tests/Commands/PgPassCommandTests.cs` para:
  - Subcomando `list` exibe mensagem quando arquivo não existe
  - Subcomando `list` exibe tabela formatada com senhas mascaradas

### Implementação para User Story 2

- [X] T013 [US2] Adicionar `IEnumerable<PgPassEntry> ListEntries(string filePath)` em `DevMaid.Core/Services/PgPassService.cs`
- [X] T014 [US2] Adicionar subcomando `list` em `DevMaid.CLI/Commands/PgPassCommand.cs`:
  - Sem argumentos nem opções
  - Exibir tabela formatada com colunas HOSTNAME, PORTA, BANCO, USUÁRIO, SENHA
  - Senha sempre substituída por `****`
  - Exibir `Nenhuma entrada configurada em pgpass.conf.` quando vazio/inexistente

**Checkpoint**: US1 e US2 devem funcionar de forma independente.

---

## Phase 5: User Story 3 — Remover uma Entrada Específica do pgpass (Prioridade: P3)

**Objetivo**: Usuário consegue executar `devmaid database pgpass remove <banco>` para remover uma entrada específica sem afetar as demais.

**Teste Independente**: Adicionar duas entradas, remover uma por `(host, porta, banco, usuario)` e verificar que somente a entrada alvo foi removida do arquivo.

### Testes para User Story 3

> **IMPORTANTE: Escrever estes testes PRIMEIRO — garantir que FALHAM antes da implementação**

- [X] T015 [P] [US3] Adicionar testes em `DevMaid.Tests/Commands/PgPassServiceTests.cs` para:
  - `RemoveEntry` remove a entrada correta e preserva as demais
  - `RemoveEntry` retorna `PgPassResult.Fail` (informativo) quando entrada não encontrada
  - `RemoveEntry` deixa arquivo inalterado quando entrada não existe
  - `RemoveEntry` trata `IOException` → `PgPassResult.Fail` com mensagem RF-013
- [X] T016 [P] [US3] Adicionar testes em `DevMaid.Tests/Commands/PgPassCommandTests.cs` para:
  - Subcomando `remove` com banco obrigatório ausente retorna código de saída `2`
  - Subcomando `remove` exibe mensagem quando entrada não encontrada

### Implementação para User Story 3

- [X] T017 [US3] Adicionar `PgPassResult RemoveEntry(PgPassEntry key, string filePath)` em `DevMaid.Core/Services/PgPassService.cs`
- [X] T018 [US3] Adicionar subcomando `remove` em `DevMaid.CLI/Commands/PgPassCommand.cs`:
  - Argumento posicional `<banco>` (obrigatório)
  - Opção `--host` / `-h` (padrão: `localhost`)
  - Opção `--port` / `-p` (padrão: `5432`)
  - Opção `--username` / `-U` (padrão: `postgres`)
  - Exibir mensagem de sucesso ou "não encontrada" conforme contrato
  - Retornar código de saída correto: `0` sempre, `1` em erro de I/O, `2` em arg inválido

**Checkpoint**: Todas as user stories devem funcionar de forma independente.

---

## Phase 6: Polish e Preocupações Transversais

**Objetivo**: Garantir qualidade, cobertura de casos de borda e validação do quickstart.

- [X] T019 [P] Validar tratamento de erros de I/O em todos os subcomandos (RF-012, RF-013): `UnauthorizedAccessException` e `IOException` geram mensagens acionáveis
- [X] T020 [P] Verificar que senhas nunca aparecem em logs ou saída de erro (restrição de segurança)
- [X] T021 Executar `dotnet build` e corrigir todos os erros de compilação
- [X] T022 Executar `dotnet test` e garantir que todos os testes passam
- [ ] T023 Validar `quickstart.md` — executar os exemplos do guia de uso para confirmar que os comandos funcionam conforme documentado

---

## Dependências e Ordem de Execução

### Dependências por Fase

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente
- **Fundação (Phase 2)**: Depende da Phase 1 — BLOQUEIA todas as user stories
- **US1 (Phase 3)**: Depende da Phase 2; sem dependência de US2/US3
- **US2 (Phase 4)**: Depende da Phase 2; sem dependência de US1/US3
- **US3 (Phase 5)**: Depende da Phase 2; sem dependência de US1/US2
- **Polish (Phase 6)**: Depende de todas as user stories desejadas estarem completas

### Dentro de Cada User Story

1. Testes escritos e **falhando** antes da implementação
2. Interface/modelos antes do serviço
3. Serviço antes do comando CLI
4. Comando CLI antes da integração final

### Oportunidades de Paralelismo

- Todos os itens marcados `[P]` dentro de uma fase podem rodar em paralelo
- T003, T004, T005, T006 (Phase 2) podem ser criados simultaneamente
- T007 e T008 (testes US1) podem ser escritos em paralelo
- US2 e US3 podem ser desenvolvidas em paralelo por desenvolvedores diferentes após a Phase 2

---

## Estratégia de Implementação

### MVP Primeiro (apenas US1)

1. Completar Phase 1: Setup
2. Completar Phase 2: Fundação
3. Completar Phase 3: US1 (`pgpass add`)
4. **PARAR E VALIDAR**: Testar US1 de forma independente
5. Demonstrar/entregar se pronto

### Entrega Incremental

1. Setup + Fundação → Base pronta
2. US1 (`add`) → Testar → Entregar (MVP!)
3. US2 (`list`) → Testar → Entregar
4. US3 (`remove`) → Testar → Entregar
5. Cada story adiciona valor sem quebrar as anteriores

---

## Notas

- `[P]` = arquivos diferentes, sem dependências entre si
- Rótulo `[Story]` mapeia task para user story específica para rastreabilidade
- Cada user story deve ser completável e testável de forma independente
- Verificar que testes falham ANTES de implementar
- Senha **nunca** em logs, traces ou saída CLI — sem exceções
- Caminhos de arquivo: `%APPDATA%\postgresql\pgpass.conf` resolvido em tempo de execução via `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`
