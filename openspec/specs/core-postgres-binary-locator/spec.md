## Propósito

Abstrair localização de binários PostgreSQL através da interface IPostgresBinaryLocator, habilitando injeção de dependência adequada e testabilidade para localização de executáveis PostgreSQL.

## Requisitos

### Requisito: IPostgresBinaryLocator abstrai localização de binários PostgreSQL
O sistema DEVE expor uma interface `IPostgresBinaryLocator` no `FurLab.Core` para abstrair a localização dos executáveis `pg_dump`, `pg_restore` e `psql`, substituindo a chamada direta à classe estática `PostgresBinaryLocator` nos comandos e health checks.

#### Cenário: Localização de pg_dump
- **QUANDO** um consumidor solicita o caminho do executável `pg_dump`
- **ENTÃO** o serviço DEVE retornar o caminho absoluto se encontrado, ou `null` se não localizado

#### Cenário: Localização de pg_restore
- **QUANDO** um consumidor solicita o caminho do executável `pg_restore`
- **ENTÃO** o serviço DEVE retornar o caminho absoluto se encontrado, ou `null` se não localizado

#### Cenário: Localização de psql
- **QUANDO** um consumidor solicita o caminho do executável `psql`
- **ENTÃO** o serviço DEVE retornar o caminho absoluto se encontrado, ou `null` se não localizado

#### Cenário: Serviço registrado no DI
- **QUANDO** `AddFurLabServices(IServiceCollection)` é chamado
- **ENTÃO** `IPostgresBinaryLocator` DEVE estar registrado como Singleton com a implementação `PostgresBinaryLocator`

#### Cenário: Health check usa IPostgresBinaryLocator via DI
- **QUANDO** o `PostgresBinaryHealthCheck` verifica a disponibilidade dos binários
- **ENTÃO** ele DEVE receber `IPostgresBinaryLocator` por construtor em vez de chamar a classe estática diretamente
