## ADDED Requirements

### Requisito: docker-postgres subcomando existe
O sistema DEVE expor o subcomando `devmaid docker postgres` que executa um container PostgreSQL local para desenvolvimento.

#### Cenário: Comando executado com sucesso
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema executa container PostgreSQL e exibe ID do container e instruções de conexão

#### Cenário: Docker não disponível
- **QUANDO** usuário executa `devmaid docker postgres` e Docker não está instalado ou em execução
- **ENTÃO** sistema exibe mensagem de erro clara: "Docker não encontrado. Instale o Docker Desktop."

### Requisito: Container usa imagem postgres:alpine
O sistema DEVE criar o container usando a imagem `postgres:alpine` para minimizar uso de espaço em disco.

#### Cenário: Container criado com imagem correta
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com imagem `postgres:alpine`

### Requisito: Container nomeado postgres-ptbr
O sistema DEVE nomear o container como `postgres-ptbr` para identificação consistente.

#### Cenário: Container criado com nome específico
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com nome `postgres-ptbr`

#### Cenário: Container já existe
- **QUANDO** usuário executa `devmaid docker postgres` e container `postgres-ptbr` já existe
- **ENTÃO** sistema inicia o container existente ao invés de criar um novo

### Requisito: Política de reinício always
O sistema DEVE configurar o container com política de reinício `always` para garantir disponibilidade após reinicialização do host.

#### Cenário: Política de reinício configurada
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com `--restart always`

### Requisito: Porta 5432 exposta
O sistema DEVE mapear a porta 5432 do container para a porta 5432 do host.

#### Cenário: Porta exposta corretamente
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com mapeamento de porta `5432:5432`

### Requisito: Senha padrão configurada
O sistema DEVE configurar a variável de ambiente `POSTGRES_PASSWORD=dev` para o container.

#### Cenário: Senha configurada
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com `-e POSTGRES_PASSWORD=dev`

### Requisito: Locale pt-BR configurado
O sistema DEVE configurar as variáveis de ambiente `LANG=pt_BR.UTF-8` e `LC_ALL=pt_BR.UTF-8` para locale brasileiro.

#### Cenário: Locale pt-BR configurado
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com `-e LANG=pt_BR.UTF-8 -e LC_ALL=pt_BR.UTF-8`

### Requisito: Volume persistente configurado
O sistema DEVE criar e usar um volume nomeado `postgres-data` para persistência dos dados.

#### Cenário: Volume persistente criado
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com volume `-v postgres-data:/var/lib/postgresql/data`

### Requisito: Logging completo de queries
O sistema DEVE configurar o PostgreSQL para logar todas as queries e queries lentas (duração mínima 0).

#### Cenário: Logging configurado
- **QUANDO** usuário executa `devmaid docker postgres`
- **ENTÃO** sistema cria container com variáveis de ambiente:
  - `-e POSTGRES_INITDB_ARGS=--log_statement=all --log_min_duration_statement=0`

### Requisito: Informações de conexão exibidas
O sistema DEVE exibir o ID do container e instruções de conexão após criar/iniciar o container com sucesso.

#### Cenário: Informações exibidas ao criar
- **QUANDO** usuário executa `devmaid docker postgres` com sucesso
- **ENTÃO** sistema exibe:
  - ID do container
  - String de conexão: `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=dev`
