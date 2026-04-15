## MODIFICADO Requirements

### Requirement: Feedback visual na fase de descoberta
O sistema DEVE exibir feedback visual durante a fase de descoberta de databases apenas quando há operação de IO real.

#### Cenário: Auto-descoberta ativa em pelo menos um servidor
- **QUANDO** pelo menos um servidor selecionado tem `fetchAllDatabases: true`
- **ENTÃO** sistema exibe spinner animado com a mensagem "Discovering databases on [servidor]..."
- **E** spinner é cancelável pelo usuário via Ctrl+C
- **E** ao concluir, prossegue para a execução das queries

#### Cenário: Databases obtidas da configuração
- **QUANDO** todos os servidores selecionados têm `fetchAllDatabases: false`
- **ENTÃO** sistema não exibe spinner na fase de descoberta
- **E** prossegue diretamente para a execução das queries
