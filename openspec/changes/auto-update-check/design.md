## Context

O FurLab é distribuído por múltiplos canais: winget (portable executable), dotnet tool (NuGet), e download manual. Atualmente não há mecanismo para notificar usuários sobre novas versões disponíveis. Usuários winget, em particular, precisam de uma forma de descobrir atualizações sem verificar manualmente o GitHub.

A solução deve ser não-intrusiva, não bloquear a execução de comandos, e funcionar offline quando necessário.

## Goals / Non-Goals

**Goals:**
- Detectar automaticamente o método de instalação (winget/dotnet-tool/manual) na primeira execução
- Verificar atualizações em background (processo separado) 1 vez ao dia
- Notificar usuário visualmente quando há atualização disponível
- Fornecer comando explícito `fur check-update` para verificação síncrona
- Permitir habilitar/desabilitar verificação automática via CLI
- Re-verificar método de instalação a cada 30 dias
- Cache de resultados para evitar chamadas API desnecessárias

**Non-Goals:**
- Download ou instalação automática de atualizações
- Verificação em tempo real (push notifications)
- Suporte a canais de release (beta, alpha)
- Notificações por email ou outros canais externos

## Decisions

### 1. Detecção de Instalação via winget list
**Decisão**: Usar `winget list --id FurLab.CLI --exact` para detectar instalação winget.
**Alternativas consideradas**:
- Path do executável (frágil, pode estar em qualquer lugar)
- Marker file durante build (winget não executa scripts pós-instalação)
- Registro do Windows (complexo, varia por versão)
**Racional**: `winget list` é a fonte mais confiável de verdade. Se FurLab.CLI está na lista, foi instalado via winget.

### 2. Processo Separado para Background Tasks
**Decisão**: Spawnar processo separado (`fur --background-task <task>`) ao invés de thread background.
**Alternativas consideradas**:
- Thread background no mesmo processo (morre se CLI terminar rápido)
- Windows Task Scheduler (overkill, complexo de configurar)
- Serviço Windows (inapropriado para CLI tool)
**Racional**: Processo separado sobrevive ao término da CLI principal, não consome recursos do processo principal, e pode ser cancelado independentemente.

### 3. Cache em Arquivo Separado (update-cache.json)
**Decisão**: Resultados de verificação armazenados em arquivo separado do config principal.
**Alternativas consideradas**:
- Armazenar no furlab.jsonc (polui config do usuário, é dados transitórios)
- Sem cache (chama API toda vez, lento e ruim para rate limits)
**Racional**: Separação de concerns - config é preferência do usuário, cache é estado temporário. Cache pode ser deletado sem perder configurações.

### 4. Timeout de 30s para Operações Externas
**Decisão**: 30 segundos para winget list e GitHub API.
**Alternativas consideradas**:
- 10s (pode ser curto demais em conexões lentas)
- 60s (muito longo, usuário pode achar que travou)
**Racional**: 30s é suficiente para maioria dos casos sem ser intrusivo. Falha silenciosa com retry em 24h.

### 5. Frequência de Verificação: 1x ao dia
**Decisão**: Verificar uma vez por dia, não a cada execução.
**Alternativas consideradas**:
- A cada execução (muito frequente, API rate limit)
- Semanal (usuário pode perder atualizações importantes)
- Manual apenas (não atende o goal de automação)
**Racional**: Balance entre frescor da informação e respeito à API do GitHub (rate limit não autenticado: 60 requests/hora).

### 6. Modelo de Dados
**Decisão**: `UpdateCheckConfig` aninhado em `UserConfig`, `UpdateCache` em arquivo separado.
```csharp
UpdateCheckConfig:
- enabled: bool
- installationMethod: "winget" | "dotnet-tool" | "manual"
- methodVerifiedAt: DateTime?
- nextCheckDue: DateTime
- checkInProgress: bool

UpdateCache:
- checkedAt: DateTime
- currentVersion: string
- latestVersion: string
- updateAvailable: bool
- releaseUrl: string
- installationMethod: string
```

## Risks / Trade-offs

### [Risco] winget list pode ser lento → Mitigação: Timeout de 30s, falha silenciosa
Se `winget list` demorar mais que 30s, abortamos e agendamos retry em 24h. Usuário não é impactado.

### [Risco] Sem internet / GitHub API indisponível → Mitigação: Falha silenciosa, retry em 24h
Não mostramos erro ao usuário. Tentamos novamente no próximo ciclo (após 24h ou próximo comando).

### [Risco] Múltiplos processos de background simultâneos → Mitigação: Lock file
Arquivo `update-check.lock` com timestamp. Se existir e tiver < 30min, não spawnamos novo processo.

### [Risco] Falso positivo (diz que é winget mas não está) → Mitigação: Re-verificação a cada 30 dias
Se usuário desinstalar winget e instalar via dotnet tool, re-detectamos em até 30 dias. Comando manual `fur check-update` também permite detecção imediata.

### [Trade-off] Notificação pode ser ignorada → Aceito
Banner no início do comando pode ser ignorado pelo usuário. Comando explícito `fur check-update` disponível para quem quer verificar proativamente.

### [Trade-off] Primeira execução pode parecer lenta → Aceito
Delay de 1-2s na primeira execução (winget list) é aceitável para configurar automaticamente.

## Migration Plan

Não há migration necessária. Este é um recurso novo que:
1. Funciona imediatamente para novas instalações
2. Funciona para instalações existentes (detecta método na primeira execução pós-update)
3. Desabilitado até ser configurado (não quebra comportamento existente)

Rollback: Usuário pode desabilitar via `fur check-update --disable` a qualquer momento.

## Open Questions

1. **Mensagens de notificação**: Confirmar que mensagens em português ("Nova versão disponível!") são apropriadas para todo público do FurLab.
2. **Cor do banner**: Usar cor padrão do Spectre.Console ou definir esquema específico para notificações?
3. **Rate limiting GitHub**: Se FurLab crescer muito (milhares de usuários), 60 req/hr pode ser insuficiente. Considerar autenticação ou header customizado?
