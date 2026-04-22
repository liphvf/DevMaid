## Por que

Usuários que instalam o FurLab via winget não têm uma forma automática de descobrir quando novas versões estão disponíveis. Atualmente, é necessário verificar manualmente no GitHub ou executar comandos de atualização sem saber se há algo novo. Este recurso adiciona verificação automática de atualizações 1 vez ao dia, detectando automaticamente o método de instalação (winget, dotnet-tool ou manual) e notificando o usuário de forma não intrusiva.

## O que Muda

- **ADICIONADO**: Comando `fur check-update` para verificação manual síncrona de atualizações
- **ADICIONADO**: Flags `--enable` e `--disable` no comando `check-update` para controlar verificação automática
- **ADICIONADO**: Verificação automática em background (processo separado) executada 1 vez ao dia
- **ADICIONADO**: Detecção automática do método de instalação (winget/dotnet-tool/manual) na primeira execução
- **ADICIONADO**: Re-verificação do método de instalação a cada 30 dias
- **ADICIONADO**: Cache de resultado da última verificação em arquivo separado
- **ADICIONADO**: Notificação visual quando há atualização disponível (mostrada no início de qualquer comando)
- **ADICIONADO**: Configuração `updateCheck` em `furlab.jsonc` para persistir preferências

## Capacidades

### Novas Capacidades
- `update-check`: Sistema de verificação automática de atualizações com detecção de método de instalação, cache, e notificação ao usuário

### Capacidades Modificadas
- `settings-user-config`: Extensão do modelo de configuração para incluir seção `updateCheck` com campos `enabled`, `installationMethod`, `methodVerifiedAt`, `nextCheckDue`, e `checkInProgress`

## Impacto

- **Configuração**: Novo arquivo `update-cache.json` em `%LocalAppData%\FurLab\` para cache de verificação
- **Performance**: Verificação em background (processo separado) não bloqueia execução dos comandos; timeout de 30s para operações externas
- **APIs externas**: Chamadas à API do GitHub (`api.github.com/repos/liphvf/FurLab/releases/latest`) e ao comando `winget list` para detecção
- **CLI**: Novos comandos `fur check-update`, `fur check-update --enable`, `fur check-update --disable`, e flags internas `--background-task detect-install-method` / `--background-task check-update`
- **UX**: Notificação visual (banner) mostrada no início de comandos quando há atualização disponível; mensagens em português
