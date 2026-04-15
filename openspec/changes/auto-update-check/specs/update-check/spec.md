## ADICIONADO Requisitos

### Requisito: Verificação automática diária de atualizações
O sistema DEVE verificar automaticamente uma vez por dia se existe nova versão disponível via NuGet API, em background, sem impactar a execução do comando principal.

#### Cenário: Verificação disparada na primeira execução do dia
- **QUANDO** o programa é iniciado
- **E** `updateCheck.lastChecked` é diferente da data atual (ou é nulo)
- **ENTÃO** sistema inicia uma task em background para consultar `https://api.nuget.org/v3-flatcontainer/furlab/index.json`
- **E** o comando principal é executado normalmente sem aguardar a verificação

#### Cenário: Verificação já realizada hoje
- **QUANDO** o programa é iniciado
- **E** `updateCheck.lastChecked` é igual à data atual
- **ENTÃO** sistema não realiza nova verificação
- **E** o comando principal é executado normalmente

#### Cenário: Verificação encontra versão mais recente
- **QUANDO** a task de background conclui
- **E** a versão retornada pela NuGet API é superior à versão atual do assembly
- **ENTÃO** sistema persiste `{ latestKnown, notified: false, lastChecked: hoje }` em `furlab.jsonc`

#### Cenário: Verificação não encontra versão mais recente
- **QUANDO** a task de background conclui
- **E** a versão retornada pela NuGet API é igual ou inferior à versão atual
- **ENTÃO** sistema persiste `{ lastChecked: hoje }` em `furlab.jsonc`
- **E** nenhuma notificação é gerada

#### Cenário: Verificação falha por erro de rede
- **QUANDO** a task de background falha (timeout, DNS, HTTP error)
- **ENTÃO** sistema falha silenciosamente
- **E** `lastChecked` não é atualizado
- **E** o comando principal não é afetado

#### Cenário: Timeout de 3 segundos
- **QUANDO** a NuGet API não responde em 3 segundos
- **ENTÃO** sistema cancela a requisição silenciosamente
- **E** `lastChecked` não é atualizado

---

### Requisito: Detecção e persistência do canal de instalação
O sistema DEVE detectar o canal de instalação (winget ou dotnet tool) uma única vez, persistir o resultado, e reutilizá-lo nas verificações subsequentes.

#### Cenário: Canal ainda não detectado
- **QUANDO** `updateCheck.channel` é nulo
- **ENTÃO** sistema executa `dotnet tool list -g` e procura por linha contendo "furlab" (case-insensitive)
- **E** se encontrado, persiste `channel: "dotnet"`
- **E** se não encontrado ou `dotnet` não disponível, persiste `channel: "winget"`

#### Cenário: Canal já detectado
- **QUANDO** `updateCheck.channel` é "winget" ou "dotnet"
- **ENTÃO** sistema reutiliza o valor persistido
- **E** não executa `dotnet tool list -g` novamente

#### Cenário: Parsing defensivo do dotnet tool list
- **QUANDO** sistema executa `dotnet tool list -g`
- **ENTÃO** procura por qualquer linha que contenha "furlab" (case-insensitive)
- **E** não depende de posição de coluna fixa na saída

---

### Requisito: Notificação na próxima invocação
O sistema DEVE exibir a notificação de atualização disponível na próxima invocação do programa, antes de executar o comando solicitado.

#### Cenário: Notificação pendente ao iniciar
- **QUANDO** o programa é iniciado
- **E** `updateCheck.notified` é `false`
- **E** `updateCheck.latestKnown` é superior à versão atual
- **ENTÃO** sistema exibe mensagem antes de executar o comando:
  `"Update available: <versão atual> → <latestKnown>. Update now? [y/N]"`

#### Cenário: Usuário confirma atualização
- **QUANDO** usuário responde "y" ou "Y" na notificação
- **E** canal é "winget"
- **ENTÃO** sistema executa `winget upgrade FurLab.CLI`
- **E** exibe mensagem de sucesso
- **E** sistema encerra sem executar o comando original

#### Cenário: Usuário confirma atualização via dotnet
- **QUANDO** usuário responde "y" ou "Y" na notificação
- **E** canal é "dotnet"
- **ENTÃO** sistema executa `dotnet tool update FurLab -g`
- **E** exibe mensagem de sucesso
- **E** sistema encerra sem executar o comando original

#### Cenário: Usuário recusa atualização
- **QUANDO** usuário responde "n", "N" ou pressiona Enter (default N)
- **ENTÃO** sistema persiste `notified: true` em `furlab.jsonc`
- **E** executa o comando original normalmente
- **E** não exibirá nova notificação até que `latestKnown` seja atualizado para versão superior

---

### Requisito: Comando `fur update check`
O sistema DEVE fornecer o subcomando `fur update check` para verificação manual e exibição do status da versão.

#### Cenário: Execução de fur update check
- **QUANDO** usuário executa `fur update check`
- **ENTÃO** sistema detecta o canal (sempre re-detecta, ignora cache)
- **E** consulta a NuGet API sincronicamente (com timeout de 10s)
- **E** exibe tabela com: versão atual, versão disponível, canal de instalação
- **E** exibe o comando para atualizar manualmente

#### Cenário: fur update check sem atualização disponível
- **QUANDO** versão disponível é igual à versão atual
- **ENTÃO** sistema exibe `"FurLab is up to date (version <versão>)"`

#### Cenário: fur update check com falha de rede
- **QUANDO** NuGet API não responde dentro do timeout
- **ENTÃO** sistema exibe `"Could not check for updates: <motivo>"`
- **E** encerra com exit code 1

---

### Requisito: Comando `fur update run`
O sistema DEVE fornecer o subcomando `fur update run` para execução direta da atualização.

#### Cenário: Execução de fur update run
- **QUANDO** usuário executa `fur update run`
- **ENTÃO** sistema detecta o canal (sempre re-detecta, ignora cache)
- **E** executa o comando de atualização correspondente ao canal
- **E** exibe a saída do comando de atualização em tempo real
- **E** encerra com o exit code do comando executado

#### Cenário: fur update run com canal desconhecido
- **QUANDO** detecção de canal falha
- **ENTÃO** sistema assume canal "winget"
- **E** exibe aviso: `"Channel detection failed, assuming winget. Run manually if needed."`
