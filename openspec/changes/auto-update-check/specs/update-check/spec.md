## ADDED Requirements

### Requirement: Sistema detecta automaticamente método de instalação na primeira execução
O sistema DEVE detectar automaticamente se o FurLab foi instalado via winget, dotnet-tool ou manual na primeira execução e persistir essa informação na configuração.

#### Scenario: Primeira execução detecta instalação winget
- **QUANDO** o usuário executa qualquer comando fur pela primeira vez (installationMethod é null)
- **E** o sistema executa `winget list --id FurLab.CLI --exact` em background
- **E** o winget retorna o pacote na lista
- **ENTÃO** o sistema define updateCheck.installationMethod = "winget"
- **E** o sistema define updateCheck.enabled = true
- **E** o sistema define updateCheck.methodVerifiedAt = data atual
- **E** o sistema salva a configuração em furlab.jsonc

#### Scenario: Primeira execução não detecta instalação winget
- **QUANDO** o usuário executa qualquer comando fur pela primeira vez (installationMethod é null)
- **E** o sistema executa `winget list --id FurLab.CLI --exact` em background
- **E** o winget não retorna o pacote (timeout ou não encontrado)
- **ENTÃO** o sistema define updateCheck.installationMethod = "manual"
- **E** o sistema define updateCheck.enabled = false
- **E** o sistema define updateCheck.methodVerifiedAt = data atual
- **E** o sistema salva a configuração em furlab.jsonc

### Requirement: Sistema verifica atualizações em background uma vez por dia
O sistema DEVE verificar atualizações em processo separado (background) uma vez por dia, não bloqueando a execução dos comandos principais.

#### Scenario: Verificação agendada é executada
- **QUANDO** o usuário executa qualquer comando fur
- **E** updateCheck.enabled = true
- **E** updateCheck.nextCheckDue <= data/hora atual
- **E** updateCheck.checkInProgress = false
- **ENTÃO** o sistema spawna processo separado `fur --background-task check-update`
- **E** define updateCheck.checkInProgress = true
- **E** continua a execução do comando principal imediatamente

#### Scenario: Verificação em progresso não duplica
- **QUANDO** o usuário executa qualquer comando fur
- **E** updateCheck.enabled = true
- **E** updateCheck.nextCheckDue <= data/hora atual
- **E** updateCheck.checkInProgress = true
- **ENTÃO** o sistema não spawna novo processo de verificação
- **E** continua a execução do comando principal normalmente

#### Scenario: Resultado da verificação é armazenado em cache
- **QUANDO** o processo de background completa a verificação
- **E** consulta GitHub API para releases/latest
- **ENTÃO** o sistema cria/atualiza update-cache.json com:
  - checkedAt: data/hora da verificação
  - currentVersion: versão atual do executável
  - latestVersion: versão mais recente do GitHub
  - updateAvailable: true se latest > current
  - releaseUrl: URL do release
  - installationMethod: método de instalação detectado
- **E** o sistema define updateCheck.nextCheckDue = data atual + 24 horas
- **E** o sistema define updateCheck.checkInProgress = false

### Requirement: Sistema notifica usuário quando há atualização disponível
O sistema DEVE notificar visualmente o usuário no início da execução de qualquer comando quando há uma atualização disponível, mostrando o método correto de atualização.

#### Scenario: Notificação de atualização winget
- **QUANDO** o usuário executa qualquer comando fur
- **E** update-cache.json existe
- **E** update-cache.updateAvailable = true
- **E** update-cache.installationMethod = "winget"
- **ENTÃO** o sistema exibe banner visual antes da execução do comando:
  - "📦 Nova versão disponível!"
  - "Instalada: {currentVersion}"
  - "Disponível: {latestVersion}"
  - "Para atualizar: winget upgrade FurLab.CLI"

#### Scenario: Notificação de atualização dotnet-tool
- **QUANDO** o usuário executa qualquer comando fur
- **E** update-cache.json existe
- **E** update-cache.updateAvailable = true
- **E** update-cache.installationMethod = "dotnet-tool"
- **ENTÃO** o sistema exibe banner visual antes da execução do comando:
  - "📦 Nova versão disponível!"
  - "Instalada: {currentVersion}"
  - "Disponível: {latestVersion}"
  - "Para atualizar: dotnet tool update -g FurLab"

#### Scenario: Notificação de atualização manual
- **QUANDO** o usuário executa qualquer comando fur
- **E** update-cache.json existe
- **E** update-cache.updateAvailable = true
- **E** update-cache.installationMethod = "manual"
- **ENTÃO** o sistema exibe banner visual antes da execução do comando:
  - "📦 Nova versão disponível!"
  - "Instalada: {currentVersion}"
  - "Disponível: {latestVersion}"
  - "Baixe em: {releaseUrl}"

### Requirement: Comando check-update permite verificação síncrona explícita
O sistema DEVE fornecer comando `fur check-update` que verifica atualizações de forma síncrona, ignorando o cache e o agendamento.

#### Scenario: Verificação síncrona bem-sucedida com atualização disponível
- **QUANDO** o usuário executa `fur check-update`
- **E** o sistema consulta GitHub API
- **E** há nova versão disponível
- **ENTÃO** o sistema exibe:
  - "Verificando atualizações..."
  - "Método de instalação: {method}"
  - "Versão atual: {current}"
  - "Última versão: {latest}"
  - Banner com instruções de atualização específicas do método
- **E** atualiza update-cache.json

#### Scenario: Verificação síncrona - já está na última versão
- **QUANDO** o usuário executa `fur check-update`
- **E** o sistema consulta GitHub API
- **E** não há nova versão (current == latest)
- **ENTÃO** o sistema exibe:
  - "Verificando atualizações..."
  - "✅ Você está na versão mais recente ({version})"
- **E** atualiza update-cache.json

### Requirement: Flags --enable e --disable controlam verificação automática
O sistema DEVE permitir habilitar ou desabilitar a verificação automática via flags do comando check-update.

#### Scenario: Habilitar verificação automática
- **QUANDO** o usuário executa `fur check-update --enable`
- **ENTÃO** o sistema define updateCheck.enabled = true
- **E** exibe: "Verificação automática de atualizações habilitada."
- **E** exibe: "Frequência: 1 vez ao dia"
- **E** exibe: "Próxima verificação: após o próximo comando"
- **E** salva a configuração

#### Scenario: Desabilitar verificação automática
- **QUANDO** o usuário executa `fur check-update --disable`
- **ENTÃO** o sistema define updateCheck.enabled = false
- **E** exibe: "Verificação automática de atualizações desabilitada."
- **E** exibe: "Use 'fur check-update --enable' para reabilitar."
- **E** salva a configuração

### Requirement: Sistema re-verifica método de instalação periodicamente
O sistema DEVE re-verificar o método de instalação a cada 30 dias para detectar mudanças (ex: usuário migrou de winget para dotnet-tool).

#### Scenario: Re-verificação após 30 dias
- **QUANDO** o usuário executa qualquer comando fur
- **E** updateCheck.installationMethod != null
- **E** updateCheck.methodVerifiedAt < (data atual - 30 dias)
- **ENTÃO** o sistema spawna processo separado `fur --background-task detect-install-method`
- **E** atualiza updateCheck.installationMethod se necessário
- **E** atualiza updateCheck.methodVerifiedAt = data atual

### Requirement: Sistema lida com falhas de forma graciosa
O sistema DEVE lidar com falhas de rede ou serviços indisponíveis de forma silenciosa, sem impactar a experiência do usuário.

#### Scenario: Falha na consulta ao GitHub API
- **QUANDO** o processo de background tenta consultar GitHub API
- **E** a consulta falha (timeout, 404, 403, sem internet)
- **ENTÃO** o sistema não exibe erro ao usuário
- **E** o sistema define updateCheck.nextCheckDue = data atual + 24 horas
- **E** o sistema define updateCheck.checkInProgress = false
- **E** tentará novamente na próxima oportunidade

#### Scenario: Timeout na detecção do winget
- **QUANDO** o processo de background executa `winget list --id FurLab.CLI --exact`
- **E** o comando excede 30 segundos
- **ENTÃO** o sistema aborta o processo
- **E** define updateCheck.installationMethod = "manual" (fallback seguro)
- **E** define updateCheck.methodVerifiedAt = data atual
- **E** define updateCheck.enabled = false
- **E** salva a configuração

### Requirement: Lock file previne processos duplicados
O sistema DEVE usar arquivo de lock para evitar múltiplos processos de verificação simultâneos.

#### Scenario: Lock file existente impede novo spawn
- **QUANDO** o sistema verifica se deve spawnar processo de background
- **E** existe arquivo `~/.furlab/update-check.lock`
- **E** o lock tem menos de 30 minutos
- **ENTÃO** o sistema não spawna novo processo
- **E** assume que outro processo está em execução

#### Scenario: Lock file antigo é ignorado
- **QUANDO** o sistema verifica se deve spawnar processo de background
- **E** existe arquivo `~/.furlab/update-check.lock`
- **E** o lock tem mais de 30 minutos (processo provavelmente morreu)
- **ENTÃO** o sistema deleta o lock file
- **E** spawna novo processo normalmente

#### Scenario: Lock file é criado ao spawnar
- **QUANDO** o sistema spawna processo de background
- **ENTÃO** o sistema cria arquivo `~/.furlab/update-check.lock` com timestamp atual

#### Scenario: Lock file é removido ao completar
- **QUANDO** o processo de background completa (sucesso ou falha)
- **ENTÃO** o sistema remove arquivo `~/.furlab/update-check.lock`
