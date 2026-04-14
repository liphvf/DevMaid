# Spec: server-auto-discover-databases

## Purpose

Define o comportamento de auto-descoberta de databases por servidor, incluindo patterns de exclusão configuráveis e a query usada para listagem.

## Requirements

### Requirement: Auto-descoberta de databases
O sistema DEVE executar uma query para listar todas as databases de um servidor quando `fetchAllDatabases` está habilitado para aquele servidor.

#### Cenário: Fetch all databases habilitado
- **QUANDO** servidor tem `fetchAllDatabases: true` no `furlab.jsonc`
- **ENTÃO** sistema executa `SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true`
- **E** filtra resultados usando `excludePatterns`
- **E** executa a query do usuário em cada database retornada sem validação prévia de acessibilidade

#### Cenário: Fetch all databases desabilitado
- **QUANDO** servidor tem `fetchAllDatabases: false` ou não especificado
- **ENTÃO** sistema usa apenas as databases listadas no campo `databases` sem qualquer conexão ao servidor nessa fase
- **E** se `databases` está vazio, usa a database padrão do usuário (postgres)

### Requirement: Patterns de exclusão configuráveis
O sistema DEVE permitir configurar patterns de exclusão por servidor para filtrar databases durante auto-descoberta.

#### Cenário: Patterns padrão
- **QUANDO** servidor tem `fetchAllDatabases: true` mas `excludePatterns` não especificado
- **ENTÃO** sistema usa patterns padrão: `["template*", "postgres"]`

#### Cenário: Patterns customizados
- **QUANDO** servidor tem `excludePatterns: ["template*", "postgres", "test_*"]`
- **ENTÃO** sistema exclui databases que match qualquer pattern
- **E** patterns suportam wildcard `*` no final

#### Cenário: Patterns vazios
- **QUANDO** servidor tem `excludePatterns: []`
- **ENTÃO** sistema não exclui nenhuma database (exceto templates do PostgreSQL)

### Requirement: Falha na query de listagem
O sistema DEVE tratar falhas na query de listagem de databases com suporte a fallback.

#### Cenário: Falha na query de listagem
- **QUANDO** query de listagem falha (ex: permissão negada)
- **ENTÃO** sistema registra erro
- **E** tenta continuar com databases configuradas explicitamente se houver
- **E** se não há databases fallback, reporta erro para aquele servidor
