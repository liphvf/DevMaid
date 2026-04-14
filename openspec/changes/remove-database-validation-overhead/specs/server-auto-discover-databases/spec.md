## REMOVIDO Requirements

### Requirement: Validação de databases acessíveis
**Razão**: A validação prévia de acessibilidade por database (`SELECT 1` individual) gera conexões excessivas ao servidor sem benefício real. Falhas de conexão durante a execução da query real já são capturadas e registradas no log de erros existente.
**Migração**: Nenhuma ação necessária. Databases inacessíveis continuarão a gerar entradas de erro no log de execução, sem interromper as demais databases.

## MODIFICADO Requirements

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

### Requirement: Falha na query de listagem
O sistema DEVE tratar falhas na query de listagem de databases com suporte a fallback.

#### Cenário: Falha na query de listagem
- **QUANDO** query de listagem falha (ex: permissão negada)
- **ENTÃO** sistema registra erro
- **E** tenta continuar com databases configuradas explicitamente se houver
- **E** se não há databases fallback, reporta erro para aquele servidor
