## ADDED Requirements

### Requirement: Validação rigorosa de identificadores SQL

O sistema DEVE validar que nomes de banco de dados são identificadores PostgreSQL válidos antes de usá-los em consultas SQL, prevenindo SQL injection.

**Nota**: Em PostgreSQL, nomes de bancos são **identificadores**, não strings. Identificadores válidos contêm apenas: letras (a-zA-Z), dígitos (0-9), e underscore (_). Devem começar com letra ou underscore.

#### Scenario: Nome de banco de dados com aspas simples
- **QUANDO** o nome do banco de dados contém uma aspas simples (ex: `test'db`)
- **ENTÃO** o sistema DEVE rejeitar o nome como inválido via `IsValidPostgreSQLIdentifier`

#### Scenario: Nome de banco de dados com double dash
- **QUANDO** o nome do banco de dados contém `--` (ex: `test--db`)
- **ENTÃO** o sistema DEVE rejeitar o nome como inválido via `IsValidPostgreSQLIdentifier`

#### Scenario: Nome de banco de dados com ponto e vírgula
- **QUANDO** o nome do banco de dados contém `;` (ex: `test;db`)
- **ENTÃO** o sistema DEVE rejeitar o nome como inválido via `IsValidPostgreSQLIdentifier`

#### Scenario: Nome de banco de dados válido (underscore)
- **QUANDO** o nome do banco de dados é `_test_db_123`
- **ENTÃO** o sistema DEVE aceitar o nome como válido via `IsValidPostgreSQLIdentifier`

#### Scenario: Nome de banco de dados válido (começa com letra)
- **QUANDO** o nome do banco de dados é `TestDatabase`
- **ENTÃO** o sistema DEVE aceitar o nome como válido via `IsValidPostgreSQLIdentifier`

#### Scenario: Nome de banco de dados inicia com dígito (inválido)
- **QUANDO** o nome do banco de dados é `123database`
- **ENTÃO** o sistema DEVE rejeitar o nome como inválido via `IsValidPostgreSQLIdentifier`

### Requirement: Validação antes de consultas SQL

O sistema DEVE validar nomes de banco de dados antes de construir e executar qualquer consulta SQL.

#### Scenario: Validação falha em CreateDatabaseIfNeeded
- **QUANDO** `CreateDatabaseIfNeeded` é chamado com nome inválido
- **ENTÃO** o sistema DEVE lançar `ArgumentException` antes de executar `psql`

#### Scenario: Validação passa e query é executada
- **QUANDO** `CreateDatabaseIfNeeded` é chamado com nome válido
- **ENTÃO** o sistema DEVE executar a consulta SQL com o nome validado
