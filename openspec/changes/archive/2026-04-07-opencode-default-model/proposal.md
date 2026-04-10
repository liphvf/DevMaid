## Por que

O OpenCode usa um arquivo de configuração (`opencode.jsonc` global ou `opencode.json` local) para definir o modelo padrão. Atualmente não há um comando no FurLab para alterar esse valor de forma rápida, forçando o usuário a editar o arquivo manualmente e lembrar o identificador exato do modelo. Este comando automatiza essa configuração com suporte a escopo global ou local e seleção interativa de modelos.

## O que Muda

- **ADICIONADO** subcomando `opencode settings default-model` na CLI do FurLab
- **ADICIONADO** flag opcional `--global` para alterar o arquivo de configuração global do OpenCode (`~/.config/opencode/opencode.jsonc`)
- **ADICIONADO** argumento opcional `<model-id>` para passar o modelo diretamente; quando omitido, um menu interativo é exibido com a lista de modelos disponíveis via `opencode models`
- **ADICIONADO** dependência `Spectre.Console.Cli 0.55.0` para renderização do menu interativo de seleção
- **ADICIONADO** lógica de resolução de arquivo de configuração: prioriza `.jsonc` sobre `.json`; cria `.jsonc` se nenhum existir

## Capacidades

### Novas Capacidades

- `opencode-default-model`: Comando CLI para definir o modelo padrão do OpenCode em escopo global ou local, com seleção interativa quando o modelo não é informado diretamente.

### Capacidades Modificadas

## Impacto

- `FurLab.CLI/Commands/OpenCodeCommand.cs`: adição do subcomando `default-model` com a lógica de resolução de arquivo e escrita do campo `model`
- `FurLab.CLI/FurLab.CLI.csproj`: adição da dependência `Spectre.Console.Cli 0.55.0`
- Nenhuma mudança em comandos existentes; o novo subcomando é aditivo
