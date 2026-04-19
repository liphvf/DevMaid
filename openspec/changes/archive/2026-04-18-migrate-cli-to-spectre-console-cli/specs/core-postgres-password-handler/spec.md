## ADICIONADO Requisitos

### Requisito: IPostgresPasswordHandler abstrai leitura interativa de senha
O sistema DEVE expor uma interface `IPostgresPasswordHandler` no `FurLab.Core` para abstrair a leitura de senha PostgreSQL de forma interativa (com máscara de caracteres), permitindo que qualquer frontend (CLI ou futuro) utilize o mecanismo de forma injetável e testável.

#### Cenário: Leitura de senha com máscara
- **QUANDO** um consumer solicita `ReadPasswordInteractively(string prompt)`
- **ENTÃO** o serviço DEVE exibir o prompt fornecido e retornar a senha digitada pelo usuário sem exibi-la na tela (masked input)

#### Cenário: Retorno de string vazia se nenhuma senha for digitada
- **QUANDO** o usuário pressiona Enter sem digitar nenhum caractere
- **ENTÃO** o serviço DEVE retornar uma `string` vazia (não `null`)

#### Cenário: Serviço registrado no DI
- **QUANDO** `AddFurLabServices(IServiceCollection)` é chamado
- **ENTÃO** `IPostgresPasswordHandler` DEVE estar registrado como Singleton com a implementação `PostgresPasswordHandler`

#### Cenário: Commands que necessitam de senha recebem o serviço por construtor
- **QUANDO** um command como `DatabaseBackupCommand`, `PgPassAddCommand` ou `DbServersAddCommand` necessita solicitar senha ao usuário
- **ENTÃO** DEVE receber `IPostgresPasswordHandler` no construtor em vez de chamar a classe estática `PostgresPasswordHandler` diretamente
