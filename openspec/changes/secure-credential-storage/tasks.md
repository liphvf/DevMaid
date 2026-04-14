## 1. Dependência e Infraestrutura

- [x] 1.1 Adicionar `Microsoft.AspNetCore.DataProtection` ao `FurLab.Core.csproj`
- [x] 1.2 Configurar DataProtection no `ServiceCollectionExtensions.cs`: `AddDataProtection().PersistKeysToFileSystem(keysDir).SetApplicationName("FurLab")`
- [x] 1.3 Definir `keysDir` como `%LocalAppData%\FurLab\keys\` (mesmo `_configFolder` de `UserConfigService`, subpasta `keys`)

## 2. ICredentialService

- [x] 2.1 Criar interface `ICredentialService` em `FurLab.Core/Interfaces/` com métodos `Encrypt(string plaintext): string` e `TryDecrypt(string? encrypted): string?`
- [x] 2.2 Criar `CredentialService` em `FurLab.Core/Services/` implementando `ICredentialService` via `IDataProtectionProvider.CreateProtector("FurLab.ServerPasswords.v1")`
- [x] 2.3 `TryDecrypt` deve retornar `null` (sem lançar exceção) em caso de falha de decriptação, campo nulo ou vazio
- [x] 2.4 Registrar `ICredentialService` → `CredentialService` como singleton no `ServiceCollectionExtensions.cs`

## 3. Modelo de Dados

- [x] 3.1 Remover campo `Password` de `ServerConfigEntry.cs`
- [x] 3.2 Adicionar campo `EncryptedPassword: string?` em `ServerConfigEntry.cs`
- [x] 3.3 Remover campo `Password` de `AddServerCommandOptions.cs`
- [x] 3.4 Adicionar método `SetEncryptedPassword(string serverName, string encryptedPassword)` em `IUserConfigService` e `UserConfigService`

## 4. QueryCommand — Resolução de Senha em Runtime

- [x] 4.1 Injetar `ICredentialService` no `QueryCommand`
- [x] 4.2 Substituir `server.Password` por `ICredentialService.TryDecrypt(server.EncryptedPassword)`
- [x] 4.3 Se `TryDecrypt` retornar `null`, acionar `PostgresPasswordHandler.ReadPassword()` interativamente
- [x] 4.4 Usar a senha obtida apenas para a sessão — não re-salvar automaticamente

## 5. SettingsCommand — Helper SelectServer()

- [x] 5.1 Extrair método privado `SelectServer(string prompt): string?` em `SettingsCommand.cs`
- [x] 5.2 Implementar guard: se nenhum servidor cadastrado, exibir aviso e retornar `null`
- [x] 5.3 Exibir `SelectionPrompt<string>` com lista de nomes de servidores
- [x] 5.4 Substituir lógica inline do `rm` pelo novo helper `SelectServer()`

## 6. SettingsCommand — Subcomando set-password

- [x] 6.1 Criar subcomando `set-password [name]` em `SettingsCommand.Build()`
- [x] 6.2 Se `name` fornecido: verificar existência do servidor; se não encontrado, erro com exit code 2
- [x] 6.3 Se `name` ausente: chamar `SelectServer()`; se retornar `null`, encerrar sem erro
- [x] 6.4 Solicitar senha via `ReadPassword()` (input mascarado já existente)
- [x] 6.5 Chamar `ICredentialService.Encrypt()` e `UserConfigService.SetEncryptedPassword()`
- [x] 6.6 Exibir: "Senha salva com segurança para '{name}'."

## 7. SettingsCommand — Refatorar add

- [x] 7.1 Remover flag `--interactive` / `-i` de `addCommand` e de `AddServerCommandOptions`
- [x] 7.2 Alterar `AddServer()` para detectar modo interativo por ausência de `--name` ou `--host` (em vez de checar `options.Interactive`)
- [x] 7.3 Atualizar `AddServerInteractive()` para pré-preencher defaults com valores já fornecidos via flags
- [x] 7.4 Remover step de coleta de senha do wizard interativo
- [x] 7.5 Substituir menu final por: "Salvar e definir senha", "Salvar e testar conexão", "Salvar sem senha", "Cancelar"
- [x] 7.6 Implementar ação "Salvar e definir senha": salvar servidor e chamar fluxo de `set-password` inline
- [x] 7.7 Remover campo `--password` / `-W` de `AddServerDirect()` (servidor salvo sem senha)

## 8. SettingsCommand — Refatorar rm e test

- [x] 8.1 Remover flag `--interactive` / `-i` de `rmCommand` e de `RemoveServerCommandOptions`
- [x] 8.2 Substituir lógica inline de seleção em `RemoveServer()` pela chamada a `SelectServer()`
- [x] 8.3 Alterar `testCommand`: tornar `--name` opcional (atualmente required com exit code 2 se ausente)
- [x] 8.4 Em `TestServerConnection()` (ou no handler do comando): se `--name` ausente, chamar `SelectServer()`

## 9. Testes

- [x] 9.1 Adicionar testes unitários para `CredentialService.Encrypt()` e `TryDecrypt()` (incluindo caso de falha)
- [x] 9.2 Atualizar testes existentes em `UserConfigServiceTests.cs` que referenciam campo `Password`
- [x] 9.3 Verificar que `AddServerCommandOptions` sem campo `Password` não quebra testes existentes
