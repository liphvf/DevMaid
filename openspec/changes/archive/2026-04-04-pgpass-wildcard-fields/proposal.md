## Por que

Desenvolvedores que usam `FurLab database pgpass` localmente precisam configurar autenticação PostgreSQL sem senha para múltiplas conexões. Atualmente, curingas (`*`) estão documentados no modelo mas bloqueados pela camada de validação. Isso força a criação de entradas redundantes como `localhost:5432:db1:postgres`, `localhost:5432:db2:postgres`, etc.

O pgpass.conf do PostgreSQL suporta nativamente `*` como curinga em qualquer campo (exceto a senha). Suportar isso melhora a experiência do desenvolvedor em ambientes locais, reduz a duplicidade de configuração e garante paridade completa com o formato nativo do pgpass.

## O que Muda

- `SecurityUtils.IsValidHost()` passará a aceitar `*` como hostname válido
- `SecurityUtils.IsValidPort()` passará a aceitar `*` como porta válida
- `SecurityUtils.IsValidUsername()` passará a aceitar `*` como usuário válido
- A validação do banco de dados já permite `*` (sem validação bloqueante)
- A documentação do contrato será atualizada para refletir o suporte a curingas em todos os campos

## Capacidades

### Novas Capacidades

- `pgpass-wildcard-validation`: As funções de validação em `SecurityUtils` passam a aceitar `*` como entrada válida para os campos host, porta e usuário, alinhando ao comportamento nativo de curingas do pgpass.conf do PostgreSQL.

### Capacidades Modificadas

- `pgpass-cli-setup`: Contrato atualizado para documentar suporte a curingas nas opções `--host`, `--port`, `--username` (banco de dados já estava documentado).

## Impacto

- **Código**: `SecurityUtils.cs` (3 métodos de validação)
- **Contrato**: `specs/011-pgpass-cli-setup/contracts/pgpass-command.md`
- **Testes**: `PgPassCommandTests.cs`, `PgPassServiceTests.cs` (novos casos de teste para curingas)
- **Compatibilidade**: Retrocompatível — apenas expande as entradas aceitas, sem quebras
