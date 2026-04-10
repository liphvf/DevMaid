## Contexto

O `OpenCodeCommand.cs` já contém infraestrutura de leitura e escrita de JSON (`LoadConfigFile` / `SaveConfigFile`) e o padrão de subcomandos aninhados (`settings > mcp-database`). O novo subcomando `default-model` se integra a essa estrutura existente.

O OpenCode usa dois tipos de arquivo de configuração:
- **Global**: `~/.config/opencode/opencode.jsonc` — afeta todas as sessões do usuário
- **Local**: `opencode.jsonc` ou `opencode.json` no diretório atual — sobrescreve o global para aquele projeto

O campo que controla o modelo é `"model"` em ambos os arquivos.

A lista de modelos disponíveis é obtida via `opencode models` (saída: uma linha por modelo, formato `provider/model-name`).

## Objetivos / Não-Objetivos

**Objetivos:**
- Adicionar subcomando `FurLab opencode settings default-model [--global] [<model-id>]`
- Quando `model-id` for omitido, exibir menu interativo com os modelos disponíveis
- Quando `--global` for passado, alterar `~/.config/opencode/opencode.jsonc`
- Quando `--global` for omitido, alterar o arquivo de configuração local do diretório atual
- Lógica de resolução de arquivo: `.jsonc` tem prioridade sobre `.json`; cria `.jsonc` se nenhum existir

**Não-Objetivos:**
- Não alterar nenhum outro campo do arquivo de configuração além de `"model"`
- Não migrar o `config.json` legado para `opencode.jsonc`

## Decisões

### Menu interativo com Spectre.Console.Cli 0.55.0

**Decisão:** adicionar `Spectre.Console.Cli 0.55.0` ao projeto.

**Alternativas consideradas:**
- `Console.ReadKey()` manual: funcional, mas requer ~100 linhas para tratar cursor, scroll e seleção em lista longa (~50+ modelos). Frágil e sem filtro por texto.
- `fzf` via processo externo: não portável, depende de instalação externa.

**Rationale:** `Spectre.Console` é aditivo — coexiste com todos os `Console.WriteLine` existentes sem mudanças. O `SelectionPrompt<T>` entrega scroll, navegação por setas e filtro por digitação prontos. Custo zero de refatoração, ganho imediato de UX.

### Resolução de arquivo de configuração

**Decisão:** lógica de prioridade aplicada tanto para escopo global quanto local:

```
existe opencode.jsonc? → usa ele
não existe, mas existe opencode.json? → usa ele
nenhum existe? → cria opencode.jsonc
```

**Rationale:** respeita o arquivo que o usuário já mantém, evitando criar duplicatas conflitantes. O `.jsonc` é o formato preferido do OpenCode (suporta comentários).

### Escopo global: path fixo

**Decisão:** `~/.config/opencode/opencode.jsonc` como caminho fixo para o escopo global.

**Rationale:** é o arquivo onde o OpenCode efetivamente lê a configuração global. O `config.json` no mesmo diretório é um arquivo legado de outro formato (usado pelo `mcp-database` atual — isso é um bug pré-existente fora do escopo desta mudança).

### Validação do model-id informado diretamente

**Decisão:** quando o usuário informa `<model-id>` como argumento, o sistema DEVE validar se ele existe na lista retornada por `opencode models` antes de gravar no arquivo.

**Alternativas consideradas:**
- Aceitar qualquer string sem validar: simples, mas silencia erros de digitação (ex: `github-copilot/claude-sonet-4.6`) que só se manifestam ao usar o OpenCode.

**Rationale:** a lista de modelos já precisa ser obtida para o menu interativo — quando o argumento é passado diretamente, basta reutilizar a mesma chamada para validar. Custo marginal zero, feedback imediato ao usuário.

**Comportamento:** se o modelo informado não constar na lista, exibir mensagem de erro com os modelos disponíveis e encerrar com código de saída diferente de zero. Nenhum arquivo é alterado.

### Argumento posicional para model-id

**Decisão:** `model-id` como argumento posicional opcional, `--global` como flag.

```
FurLab opencode settings default-model github-copilot/claude-sonnet-4.6 --global
FurLab opencode settings default-model --global   ← abre menu
FurLab opencode settings default-model            ← abre menu, escopo local
```

**Rationale:** consistente com convenções de CLI (flags modificam comportamento, argumentos são dados). `System.CommandLine` trata a ordem como transparente.

## Riscos / Trade-offs

- **Spectre.Console em ambiente sem terminal interativo** (ex: CI, pipe): `SelectionPrompt` pode falhar ou travar se não houver TTY. Mitigação: o menu só é exibido quando `model-id` é omitido; em ambientes automatizados, o usuário sempre passará o argumento diretamente.
- **`opencode models` indisponível**: se o OpenCode não estiver instalado ou no PATH, o comando falha ao tentar popular o menu. Mitigação: tratar a exceção com mensagem de erro clara.
- **Arquivo `.jsonc` com comentários**: `System.Text.Json` não suporta comentários por padrão. Se o arquivo global já tiver comentários, o parse falhará. Mitigação: usar `JsonDocumentOptions` com `CommentHandling = JsonCommentHandling.Skip` na leitura.

## Plano de Migração

Mudança puramente aditiva — nenhum comando existente é alterado. Nenhum passo de migração necessário.


