## Por que

O FurLab não notifica o usuário quando uma nova versão está disponível. Usuários que instalam via winget ou dotnet tool precisam verificar manualmente se há atualizações, o que resulta em versões desatualizadas em uso. A verificação automática uma vez por dia resolve isso de forma não intrusiva.

## O que Muda

- Na primeira execução do dia, o programa verifica em background se existe uma nova versão disponível via NuGet API
- O canal de instalação (winget ou dotnet tool) é detectado automaticamente via `dotnet tool list -g` e persistido no `furlab.jsonc`
- Quando uma nova versão é encontrada, a notificação é exibida na **próxima** invocação do programa (não na atual), antes de executar o comando
- O usuário é perguntado se deseja atualizar agora; se sim, o comando correto é executado (`winget upgrade FurLab.CLI` ou `dotnet tool update FurLab -g`) e o programa encerra — o usuário re-roda o comando original na nova versão
- Se o usuário recusar, não será notificado novamente até que uma versão mais nova seja detectada (no mínimo no dia seguinte)
- Dois novos subcomandos são adicionados: `fur update check` (exibe status da versão) e `fur update run` (executa a atualização diretamente)
- A verificação falha silenciosamente em caso de erro de rede ou timeout (3s)

## Capacidades

### Novas Capacidades

- `update-check`: Verificação automática diária de atualizações disponíveis, detecção do canal de instalação, notificação na próxima invocação e comandos `fur update check` / `fur update run`

### Capacidades Modificadas

- `settings-user-config`: A estrutura do `furlab.jsonc` é estendida com o campo `updateCheck` para persistir estado da verificação (canal, última verificação, versão disponível, flag de notificação)

## Impacto

- **`FurLab.Core/Models/UserConfig.cs`**: novo campo `UpdateCheck` do tipo `UpdateCheckState`
- **`FurLab.Core/Models/UpdateCheckState.cs`**: novo model com campos `channel`, `lastChecked`, `latestKnown`, `notified`
- **`FurLab.Core/Interfaces/IUpdateCheckService.cs`**: nova interface
- **`FurLab.Core/Services/UpdateCheckService.cs`**: implementação — detecção de canal, consulta NuGet API, persistência
- **`FurLab.CLI/Commands/UpdateCommand.cs`**: novos subcomandos `check` e `run`
- **`FurLab.CLI/Program.cs`**: integração da notificação no startup e do background task no teardown
- **Dependência externa**: `api.nuget.org` (HTTP GET, sem novo pacote NuGet necessário — usa `HttpClient` nativo)
