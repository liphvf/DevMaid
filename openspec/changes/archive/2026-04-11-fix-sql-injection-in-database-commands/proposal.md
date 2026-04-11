## Por que

A injeção de SQL é uma vulnerabilidade crítica de segurança que permite execução de comandos SQL arbitrários no servidor PostgreSQL. O código atual em `DatabaseCommand.cs:730` interpola diretamente o nome do banco de dados em consultas SQL sem escape adequado, permitindo ataques de injeção.

## O que Muda

- **FurLab.CLI/Commands/DatabaseCommand.cs**: Corrigir interpolação de `databaseName` em consultas SQL usando escape manual de caracteres especiais
- **FurLab.Core/Services/DatabaseService.cs**: Aplicar a mesma correção para Consistência (esse arquivo também contém SQL strings com interpolação)

## Capacidades

### Novas Capacidades
- `sql-injection-fix`: Correção de vulnerabilidade de SQL injection em todos os comandos de banco de dados

### Capacidades Modificadas
- (nenhuma)

## Impacto

- **Arquivos afetados**: 
  - `FurLab.CLI/Commands/DatabaseCommand.cs`
  - `FurLab.Core/Services/DatabaseService.cs`
- **Segurança**: Elimina vulnerabilidade crítica de SQL injection
- **Compatibilidade**: Não há breaking changes - apenas correção de segurança
