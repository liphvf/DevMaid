## Context

### Estado Atual
O código atual em `DatabaseCommand.cs:730` e `DatabaseService.cs:430` utiliza interpolação de strings para construir consultas SQL:

```csharp
Arguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'\""
```

**Nota importante**: O `databaseName` aqui é um **identificador** de banco de dados, não uma string SQL arbitrária. Os nomes de bancos PostgreSQL devem seguir regras específicas de identificadores.

### Regras de Identificadores PostgreSQL
- Identificadores comuns: letras (a-z), dígitos (0-9), underscore (_)
- Devem começar com letra ou underscore (não dígito)
- Comprimento máximo: 63 caracteres
- Não podem conter caracteres especiais como `'`, `"`, `;`, `-`, etc.

### Restrições
- O psql CLI do PostgreSQL não suporta parâmetros bind, apenas interpola strings diretamente
- A validação `SecurityUtils.IsValidPostgreSQLIdentifier` já existe e implementa a regex correta

## Goals / Non-Goals

**Goals:**
- Garantir que apenas identificadores válidos de banco de dados sejam usados nas consultas
- Aplicar validação `IsValidPostgreSQLIdentifier` em ambos os arquivos (CLI e Core)
- Manter compatibilidade com o comportamento atual

**Non-Goals:**
- Refatorar toda a arquitetura de comandos de banco de dados
- Alterar a validação `IsValidPostgreSQLIdentifier` existente para outros propósitos
- Adicionar parameterized queries (não suportado pelo psql CLI)

## Decisões

### 1. Validação Rigorosa ao invés de Escape

**Decisão**: Usar validação com `IsValidPostgreSQLIdentifier` ao invés de escape.

**Rationale**: 
- Nomes de bancos PostgreSQL são **identificadores**, não strings SQL
- Se um nome passa na validação `IsValidPostgreSQLIdentifier`, ele contém apenas caracteres seguros
- O escape `Replace("'", "''")` não faz sentido aqui porque identificadores válidos não contêm `'`
- A validação é mais simples e à prova de erros que tentativa de escape

### 2. Onde Aplicar a Validação

**Decisão**: Aplicar validação `IsValidPostgreSQLIdentifier` no ponto de entrada, antes de construir as queries.

**Locais**:
- `DatabaseCommand.cs`: `CreateDatabaseIfNeeded()` - já tem validação via `ValidateConnectionParameters`, mas `databaseName` não é validado ali
- `DatabaseService.cs`: `CreateDatabaseIfNeededAsync()` - não tem validação explícita

### 3. Alternativas Consideradas

| Alternativa | Problema |
|-------------|----------|
| Escape com `Replace("'", "''")` | Não faz sentido para identificadores; se o nome tem `'`, ele não deveria passar na validação |
| Usar quoted identifiers (`"dbname"`) | Requer change na query; mais complexo |
| Parâmetros bind | Não suportados pelo psql CLI |

## Riscos / Trade-offs

| Risco | Mitigação |
|-------|-----------|
| Validação bypassada | A validação é chamada explicitamente antes das queries |
| Nomes com caracteres inválidos | Retorna erro claro ao usuário |

## Plano de Migração

1. **Fase 1**: Corrigir `DatabaseCommand.cs`
   - Adicionar chamada `SecurityUtils.IsValidPostgreSQLIdentifier(databaseName)` em `CreateDatabaseIfNeeded`
   - Se inválido, lançar `ArgumentException` com mensagem clara
   
2. **Fase 2**: Corrigir `DatabaseService.cs`
   - Adicionar validação similar em `CreateDatabaseIfNeededAsync`
   - Usar `Regex.IsMatch(databaseName, @"^[a-zA-Z_][a-zA-Z0-9_]*$")` diretamente ou chamar método existente

3. **Verificação**:
   - Compilar o projeto
   - Executar testes existentes
   - Teste com nome de banco inválido deve retornar erro

## Perguntas em Aberto

- (nenhuma)
