## Por que

As senhas dos servidores de banco de dados são atualmente armazenadas em texto puro no `furlab.jsonc`, conforme explicitamente documentado no código (`"stored in plain text for dev environments"`). Isso representa um risco de segurança real: qualquer leitura acidental do arquivo expõe credenciais de produção. A mudança adota `Microsoft.AspNetCore.DataProtection` para encriptar senhas em repouso, aproveitando DPAPI no Windows e abrindo caminho para suporte crossplataforma futuro.

## O que Muda

- **REMOVIDO**: campo `Password` em claro no `ServerConfigEntry` e no `furlab.jsonc`
- **ADICIONADO**: campo `EncryptedPassword` (blob base64) no `ServerConfigEntry`
- **ADICIONADO**: `ICredentialService` / `CredentialService` — camada de encriptação/decriptação via DataProtection
- **ADICIONADO**: chaves de criptografia gerenciadas automaticamente em `%LocalAppData%\FurLab\keys\`
- **ADICIONADO**: subcomando `fur settings db-servers set-password <name>` — define/redefine a senha de um servidor
- **MODIFICADO**: `fur settings db-servers add` — remove flag `-i`; modo interativo ativado automaticamente quando `--name` ou `--host` ausentes; senha removida do wizard, substituída por ação no menu final
- **MODIFICADO**: `fur settings db-servers rm` — remove flag `-i`; sem `--name` aciona `SelectServer()` automaticamente
- **MODIFICADO**: `fur settings db-servers test` — sem `--name` aciona `SelectServer()` automaticamente (antes retornava erro)
- **ADICIONADO**: helper `SelectServer()` compartilhado por `rm`, `test` e `set-password`
- **MODIFICADO**: resolução de senha em runtime — se decriptação falhar, solicita senha interativamente ao usuário (não re-salva automaticamente)

## Capacidades

### Novas Capacidades

- `credential-storage`: Encriptação e decriptação de senhas de servidores via `Microsoft.AspNetCore.DataProtection`; key ring gerenciado em `%LocalAppData%\FurLab\keys\`; fallback para prompt interativo quando senha não disponível
- `db-servers-set-password`: Subcomando `fur settings db-servers set-password [name]` para definir ou redefinir a senha encriptada de um servidor; sem argumento, exibe seleção interativa de servidores

### Capacidades Modificadas

- `settings-db-servers`: Remoção da flag `-i` do `add`; modo interativo automático no `add`, `rm` e `test` quando argumentos insuficientes; senha removida do fluxo `add` (gerenciada via `set-password`); **BREAKING** campo `password` removido do `furlab.jsonc`

## Impacto

- **`FurLab.Core.csproj`**: nova dependência `Microsoft.AspNetCore.DataProtection`
- **`FurLab.Core/Models/ServerConfigEntry.cs`**: campo `Password` removido, `EncryptedPassword: string?` adicionado
- **`FurLab.Core/Services/`**: `ICredentialService` + `CredentialService` novos; `ServiceCollectionExtensions` registra DataProtection e `ICredentialService`; `UserConfigService` ganha método `SetEncryptedPassword()`
- **`FurLab.CLI/Commands/SettingsCommand.cs`**: `AddServer` refatorado (sem `-i`, interativo por detecção); `set-password` adicionado; `test` e `rm` ganham fallback via `SelectServer()`; helper `SelectServer()` extraído
- **`FurLab.CLI/Commands/QueryCommand.cs`**: usa `ICredentialService.TryDecrypt()` em vez de `server.Password` diretamente
- **`FurLab.CLI/CommandOptions/AddServerCommandOptions.cs`**: campo `Interactive` removido; campo `Password` removido
- **Formato `furlab.jsonc`**: campo `password` **BREAKING** substituído por `encryptedPassword`; usuários existentes precisam rodar `fur settings db-servers set-password` para re-cadastrar senhas
