## 1. Dependência

- [ ] 1.1 Adicionar `Spectre.Console.Cli 0.55.0` ao `DevMaid.CLI/DevMaid.CLI.csproj` via `dotnet add package`

## 2. Lógica de Resolução de Arquivo

- [ ] 2.1 Extrair método `ResolveConfigPath(string directory, bool global)` em `OpenCodeCommand.cs` que implementa a prioridade: `.jsonc` > `.json` > cria `.jsonc`
- [ ] 2.2 Atualizar `LoadConfigFile` para usar `JsonDocumentOptions` com `CommentHandling = JsonCommentHandling.Skip` na leitura de arquivos `.jsonc`

## 3. Obtenção da Lista de Modelos

- [ ] 3.1 Implementar método `GetAvailableModels()` que executa `opencode models` e retorna `IReadOnlyList<string>` com uma entrada por linha
- [ ] 3.2 Tratar `InvalidOperationException` (opencode não encontrado no PATH) com mensagem de erro clara

## 4. Menu Interativo

- [ ] 4.1 Implementar método `SelectModelInteractively(IReadOnlyList<string> models)` usando `Spectre.Console.AnsiConsole.Prompt` com `SelectionPrompt<string>` configurado com título e lista de modelos
- [ ] 4.2 Tratar cancelamento (Esc / Ctrl+C) retornando `null` e encerrando sem alterar arquivos

## 5. Subcomando `default-model`

- [ ] 5.1 Criar `Command("default-model")` com argumento posicional opcional `<model-id>` e flag `--global` em `OpenCodeCommand.Build()`
- [ ] 5.2 Implementar `SetAction` do subcomando: obter lista de modelos via `GetAvailableModels()`, validar `model-id` quando informado diretamente (exibir erro e encerrar se inválido), ou abrir menu interativo quando omitido
- [ ] 5.3 Após obter modelo válido: resolver caminho via `ResolveConfigPath`, carregar config, definir `"model"`, salvar e exibir caminho do arquivo alterado
- [ ] 5.4 Adicionar o subcomando `default-model` ao `settingsCommand`

## 6. Testes

- [ ] 6.1 Adicionar testes unitários para `ResolveConfigPath`: cenários com `.jsonc` existente, `.json` existente, nenhum arquivo, e escopo global
- [ ] 6.2 Adicionar teste para leitura de arquivo `.jsonc` com comentários (verificar que não falha)
- [ ] 6.3 Adicionar testes para validação de model-id: modelo válido na lista, modelo inválido não na lista
- [ ] 6.4 Executar `dotnet build` e garantir que não há erros ou warnings
