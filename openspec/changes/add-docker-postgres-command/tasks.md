## 1. Estrutura do Projeto

- [x] 1.1 Criar arquivo `DockerCommand.cs` em `DevMaid.CLI/Commands/`
- [x] 1.2 Criar classe `DockerPostgresCommand` (pode ser no mesmo arquivo ou separado)
- [x] 1.3 Criar `DockerService.cs` em `DevMaid.CLI/Services/` para operações Docker

## 2. Implementação do DockerCommand

- [x] 2.1 Implementar método `Build()` que retorna Command com `docker` e subcomando `postgres`
- [x] 2.2 Adicionar documentação "Docker utilities."

## 3. Implementação do DockerPostgresCommand

- [x] 3.1 Implementar método `BuildPostgresCommand()` que cria subcomando `postgres`
- [x] 3.2 Adicionar validação de Docker disponível (`docker info`)
- [x] 3.3 Implementar lógica para verificar se container `postgres-ptbr` já existe
- [x] 3.4 Implementar criação de container com parâmetros:
  - Imagem: `postgres:alpine`
  - Nome: `postgres-ptbr`
  - Restart: `always`
  - Porta: `5432:5432`
  - Senha: `POSTGRES_PASSWORD=dev`
  - Locale: `LANG=pt_BR.UTF-8`, `LC_ALL=pt_BR.UTF-8`
  - Volume: `postgres-data:/var/lib/postgresql/data`
- [x] 3.5 Exibir ID do container e instruções de conexão após sucesso

## 4. Implementação do DockerService

- [x] 4.1 Criar método `IsDockerAvailable()` que executa `docker info`
- [x] 4.2 Criar método `ContainerExists(containerName)` para verificar se container existe
- [x] 4.3 Criar método `StartContainer(containerName)` para iniciar container existente
- [x] 4.4 Criar método `CreatePostgresContainer()` que executa `docker run` com parâmetros corretos
- [x] 4.5 Tratar erros comuns (Docker não encontrado, porta em uso, container em estado inválido)

## 5. Integração

- [x] 5.1 Registrar `DockerCommand.Build()` em `Program.cs`
- [x] 5.2 Adicionar imports necessários (`DevMaid.CLI.Commands`, `DevMaid.CLI.Services`)

## 6. Testes Manuais

- [ ] 6.1 Testar com Docker Desktop parado (verificar mensagem de erro)
- [ ] 6.2 Testar primeiro provisionamento completo
- [ ] 6.3 Testar com container já existente (verificar que inicia ao invés de criar)
- [ ] 6.4 Verificar conexão com PostgreSQL após provisionamento
