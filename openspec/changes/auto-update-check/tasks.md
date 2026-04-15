## 1. Models e Interfaces (Core)

- [ ] 1.1 Criar `FurLab.Core/Models/UpdateCheckState.cs` com campos `Channel` (string?), `LastChecked` (string?), `LatestKnown` (string?) e `Notified` (bool)
- [ ] 1.2 Adicionar campo `UpdateCheck` do tipo `UpdateCheckState?` em `FurLab.Core/Models/UserConfig.cs`
- [ ] 1.3 Criar `FurLab.Core/Interfaces/IUpdateCheckService.cs` com métodos `Task<string?> GetLatestVersionAsync(CancellationToken)`, `Task<string> DetectChannelAsync()` e `Task CheckAndPersistAsync(CancellationToken)`

## 2. Serviço de Verificação (Core)

- [ ] 2.1 Criar `FurLab.Core/Services/UpdateCheckService.cs` implementando `IUpdateCheckService`
- [ ] 2.2 Implementar `DetectChannelAsync`: executa `dotnet tool list -g`, busca linha com "furlab" (case-insensitive), retorna "dotnet" ou "winget"
- [ ] 2.3 Implementar `GetLatestVersionAsync`: consulta `https://api.nuget.org/v3-flatcontainer/furlab/index.json`, extrai última versão do array, timeout 3s
- [ ] 2.4 Implementar `CheckAndPersistAsync`: verifica `lastChecked == hoje` (skip se sim), detecta canal se `channel == null`, consulta NuGet, persiste resultado em `furlab.jsonc` via `IUserConfigService`
- [ ] 2.5 Registrar `IUpdateCheckService` → `UpdateCheckService` no `ServiceCollectionExtensions.cs`

## 3. Integração no Startup (CLI)

- [ ] 3.1 Em `Program.cs`, após construir o host, ler `updateCheck` do `furlab.jsonc` e verificar se há notificação pendente (`notified == false && latestKnown > versão atual`)
- [ ] 3.2 Se notificação pendente: exibir prompt antes de executar o comando (`"Update available: X → Y. Update now? [y/N]"`)
- [ ] 3.3 Se usuário confirmar: detectar canal, executar `winget upgrade FurLab.CLI` ou `dotnet tool update FurLab -g`, encerrar
- [ ] 3.4 Se usuário recusar: persistir `notified: true` e continuar execução normal
- [ ] 3.5 Após `rootCommand.Parse(args).Invoke(...)`, iniciar task de background via `IUpdateCheckService.CheckAndPersistAsync` com timeout de 3s e await antes de encerrar o processo

## 4. Comando `fur update` (CLI)

- [ ] 4.1 Criar `FurLab.CLI/Commands/UpdateCommand.cs` com subcomandos `check` e `run`
- [ ] 4.2 Implementar `fur update check`: re-detecta canal (ignora cache), consulta NuGet com timeout 10s, exibe versão atual, versão disponível, canal e comando de atualização manual
- [ ] 4.3 Implementar `fur update run`: re-detecta canal (ignora cache), executa `winget upgrade FurLab.CLI` ou `dotnet tool update FurLab -g`, propaga exit code
- [ ] 4.4 Registrar `UpdateCommand.Build()` no `RootCommand` em `Program.cs`

## 5. UserConfigService — suporte a UpdateCheckState

- [ ] 5.1 Atualizar `ValidateAndApplyDefaults` em `UserConfigService.cs` para inicializar `UpdateCheck` com valores padrão quando ausente
- [ ] 5.2 Adicionar método `SaveUpdateCheckState(UpdateCheckState state)` na interface `IUserConfigService` e implementação
- [ ] 5.3 Adicionar método `GetUpdateCheckState()` na interface e implementação

## 6. Testes

- [ ] 6.1 Criar `FurLab.Tests/Commands/UpdateCommandTests.cs` com testes para `fur update check` (versão atual, versão disponível, falha de rede)
- [ ] 6.2 Criar `FurLab.Tests/Services/UpdateCheckServiceTests.cs` com testes para detecção de canal (dotnet encontrado, dotnet não encontrado) e parsing da NuGet API
- [ ] 6.3 Testar cenário de notificação pendente: `notified: false`, versão superior disponível → prompt exibido
- [ ] 6.4 Testar cenário de recusa: `notified` atualizado para `true` após resposta "n"
