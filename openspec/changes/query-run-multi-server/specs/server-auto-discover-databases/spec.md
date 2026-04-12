## ADDED Requirements

### Requirement: Auto-descoberta de databases
O sistema DEVE executar uma query para listar todas as databases acessíveis de um servidor quando `fetchAllDatabases` está habilitado para aquele servidor.

#### Cenário: Fetch all databases habilitado
- **QUANDO** servidor tem `fetchAllDatabases: true` no `furlab.jsonc`
- **ENTÃO** sistema executa `SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true`
- **E** filtra resultados usando `excludePatterns`
- **E** executa a query do usuário em cada database retornada

#### Cenário: Fetch all databases desabilitado
- **QUANDO** servidor tem `fetchAllDatabases: false` ou não especificado
- **ENTÃO** sistema usa apenas as databases listadas no campo `databases`
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

### Requirement: Query de listagem de databases
O sistema DEVE usar a query correta para listar databases, excluindo templates e aplicando patterns.

#### Cenário: Query executada com sucesso
- **QUANDO** sistema conecta ao servidor para auto-descoberta
- **ENTÃO** executa query de listagem
- **E** retorna lista de databases acessíveis
- **E** aplica filtros de excludePatterns

#### Cenário: Falha na query de listagem
- **QUANDO** query de listagem falha (ex: permissão negada)
- **ENTÃO** sistema registra erro
- **E** tenta continuar com databases configuradas explicitamente se houver
- **E** se não há databases fallback, reporta erro para aquele servidor

### Requirement: Validação de databases acessíveis
O sistema DEVE validar que cada database descoberta é acessível antes de executar a query do usuário.

#### Cenário: Database acessível
- **QUANDO** sistema tenta conectar a uma database descoberta
- **ENTÃO** se conexão funciona, executa query do usuário
- **E** inclui resultado no CSV consolidado

#### Cenário: Database inacessível
- **QUANDO** sistema tenta conectar a uma database descoberta
- **ENTÃO** se conexão falha, registra erro no CSV
- **E** continua com próxima database
