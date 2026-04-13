# Spec: query-run-interactive

## Purpose

Define o comportamento do prompt interativo de seleção de servidores ao executar o comando `query run`, incluindo exibição de informações dos servidores e integração com os parâmetros de query.

## Requirements

### Requirement: Seleção interativa de servidores
O sistema DEVE exibir um prompt interativo de seleção múltipla com todos os servidores configurados no `furlab.jsonc` quando o comando `query run` for executado sem parâmetros de servidor explícitos. Todos os servidores DEVEM estar pré-selecionados por padrão.

#### Cenário: Servidores configurados disponíveis
- **QUANDO** usuário executa `query run` e existem servidores configurados no `furlab.jsonc`
- **ENTÃO** sistema exibe MultiSelectionPrompt com todos os servidores listados
- **E** todos os servidores estão pré-selecionados
- **E** usuário pode desmarcar servidores que não deseja usar
- **E** usuário pode confirmar a seleção para prosseguir

#### Cenário: Nenhum servidor configurado
- **QUANDO** usuário executa `query run` e não existem servidores configurados no `furlab.jsonc`
- **ENTÃO** sistema exibe mensagem: "Nenhum servidor encontrado, utilize o comando fur settings db-servers para configurar ou informe os parâmetros obrigatórios."
- **E** sistema encerra com exit code 2

#### Cenário: Usuário desmarca todos os servidores
- **QUANDO** usuário desmarca todos os servidores no MultiSelectionPrompt e confirma
- **ENTÃO** sistema exibe mensagem: "Nenhum servidor selecionado. Execução cancelada."
- **E** sistema encerra com exit code 0

#### Cenário: Usuário cancela a seleção
- **QUANDO** usuário pressiona Ctrl+C durante o MultiSelectionPrompt
- **ENTÃO** sistema encerra com exit code 130

### Requirement: Exibição de informações do servidor na lista
O sistema DEVE exibir informações relevantes para cada servidor na lista de seleção, incluindo nome, host, porta e databases configuradas.

#### Cenário: Servidor com databases específicas
- **QUANDO** servidor tem databases configuradas explicitamente
- **ENTÃO** lista exibe: "nome (host:porta) - databases: db1, db2"

#### Cenário: Servidor com auto-descoberta
- **QUANDO** servidor tem `fetchAllDatabases: true`
- **ENTÃO** lista exibe: "nome (host:porta) - auto-discover"

### Requirement: Integração com parâmetros de query
O sistema DEVE aceitar query via `-c` ou `-i` junto com a seleção interativa de servidores.

#### Cenário: Query inline com seleção interativa
- **QUANDO** usuário executa `query run -c "SELECT 1"`
- **ENTÃO** sistema exibe MultiSelectionPrompt de servidores
- **E** executa a query nos servidores selecionados

#### Cenário: Query de arquivo com seleção interativa
- **QUANDO** usuário executa `query run -i query.sql`
- **ENTÃO** sistema exibe MultiSelectionPrompt de servidores
- **E** executa a query nos servidores selecionados
