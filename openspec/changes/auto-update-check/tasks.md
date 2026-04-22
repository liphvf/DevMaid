## 1. Modelos e Configuração

- [x] 1.1 Criar classe `UpdateCheckConfig` em `FurLab.Core.Models`
- [x] 1.2 Criar classe `UpdateCache` em `FurLab.Core.Models`
- [x] 1.3 Estender `UserConfig` para incluir propriedade `UpdateCheck`
- [x] 1.4 Estender `UserConfigService` com métodos `GetUpdateCheckConfig()`, `SaveUpdateCheckConfig()`, `SetInstallationMethod()`
- [x] 1.5 Atualizar `IUserConfigService` interface com novos métodos

## 2. Serviços de Verificação

- [x] 2.1 Criar interface `IUpdateCheckService` em `FurLab.Core.Interfaces`
- [x] 2.2 Criar `UpdateCheckService` em `FurLab.Core.Services`
- [x] 2.3 Implementar `GetLatestVersionFromGitHub()` com HttpClient
- [x] 2.4 Implementar `DetectInstallationMethodAsync()` usando winget list
- [x] 2.5 Implementar `CheckForUpdateAsync()` com comparação de versões
- [x] 2.6 Implementar `LoadUpdateCache()` e `SaveUpdateCache()`
- [x] 2.7 Registrar `UpdateCheckService` no DI container

## 3. Background Task Runner

- [x] 3.1 Criar `BackgroundTaskRunner` em `FurLab.Core.Services`
- [x] 3.2 Implementar `RunDetectInstallMethodTaskAsync()`
- [x] 3.3 Implementar `RunCheckUpdateTaskAsync()`
- [x] 3.4 Implementar lock file mechanism (`update-check.lock`)
- [x] 3.5 Implementar timeout de 30s para operações externas

## 4. CLI - Comando Check-Update

- [x] 4.1 Criar `CheckUpdateSettings` em `FurLab.CLI.Commands.CheckUpdate`
- [x] 4.2 Criar `CheckUpdateCommand` com lógica síncrona
- [x] 4.3 Implementar flag `--enable`
- [x] 4.4 Implementar flag `--disable`
- [x] 4.5 Implementar exibição de resultado (com/sem atualização)
- [x] 4.6 Registrar comando no `Program.cs`

## 5. CLI - Background Tasks

- [x] 5.1 Adicionar parsing de `--background-task` no `Program.cs`
- [x] 5.2 Implementar handler para `detect-install-method`
- [x] 5.3 Implementar handler para `check-update`
- [x] 5.4 Garantir que background tasks não executam lógica normal do CLI

## 6. Integração com Fluxo Principal

- [x] 6.1 Criar `UpdateCheckMiddleware` ou serviço de notificação
- [x] 6.2 Implementar verificação de `nextCheckDue` no início de comandos
- [x] 6.3 Implementar spawn de processo background quando necessário
- [x] 6.4 Implementar exibição de banner quando `updateAvailable = true`
- [x] 6.5 Implementar re-verificação de método a cada 30 dias
- [x] 6.6 Adicionar banner no pipeline do Spectre.Console (antes da execução)

## 7. UI e Mensagens

- [x] 7.1 Criar template de banner para notificação de atualização
- [x] 7.2 Implementar mensagens específicas por método (winget/dotnet-tool/manual)
- [x] 7.3 Implementar mensagens de enable/disable
- [x] 7.4 Estilizar banner com Spectre.Console (cores, emoji)
- [x] 7.5 Garantir que mensagens estão em português

## 8. Testes (opcional para MVP)

- [ ] 8.1 Criar testes unitários para `UpdateCheckService`
- [ ] 8.2 Criar testes para parsing de versão (semver)
- [ ] 8.3 Criar testes para `BackgroundTaskRunner`
- [ ] 8.4 Criar testes de integração para `CheckUpdateCommand`
- [ ] 8.5 Criar testes para extensão de `UserConfigService`

## 9. Testes Manuais e Validação (para validação completa)

- [ ] 9.1 Testar detecção de instalação winget na primeira execução
- [ ] 9.2 Testar detecção de instalação manual (fallback)
- [ ] 9.3 Testar verificação em background (não bloqueia)
- [ ] 9.4 Testar notificação quando há atualização
- [ ] 9.5 Testar comando `fur check-update` síncrono
- [ ] 9.6 Testar `--enable` e `--disable`
- [ ] 9.7 Testar lock file (impedir processos duplicados)
- [ ] 9.8 Testar falha silenciosa (sem internet)
- [ ] 9.9 Testar timeout de 30s

## 10. Documentação

- [ ] 10.1 Atualizar `README.md` com informações sobre verificação de atualizações
- [ ] 10.2 Documentar comando `fur check-update`
- [ ] 10.3 Atualizar exemplo `furlab.example.jsonc` com seção `updateCheck`
