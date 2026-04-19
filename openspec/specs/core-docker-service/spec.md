## Propósito

Abstrair operações de containers Docker através da interface IDockerService, habilitando injeção de dependência adequada e testabilidade para funcionalidades relacionadas ao Docker.

## Requisitos

### Requisito: IDockerService abstrai operações com containers Docker
O sistema DEVE expor uma interface `IDockerService` no `FurLab.Core` para abstrair operações de verificação de status e gerenciamento de containers Docker, removendo a dependência direta da classe estática `DockerService` no `FurLab.CLI`.

#### Cenário: Verificação de status do Docker
- **QUANDO** um consumidor solicita `GetDockerStatusAsync()`
- **ENTÃO** o serviço DEVE retornar um `DockerStatus` indicando se o Docker está em execução, instalado mas parado, ou não instalado

#### Cenário: Criação ou inicialização de container PostgreSQL
- **QUANDO** um consumidor solicita a inicialização de um container PostgreSQL com as opções fornecidas
- **ENTÃO** o serviço DEVE verificar se o container já existe (e iniciá-lo) ou criar um novo container com a imagem e configurações especificadas

#### Cenário: Serviço registrado no DI
- **QUANDO** `AddFurLabServices(IServiceCollection)` é chamado
- **ENTÃO** `IDockerService` DEVE estar registrado como Singleton com a implementação `DockerService`

---

### Requisito: DockerConstants expõe configurações padrão do container PostgreSQL
O sistema DEVE manter as constantes de configuração do container Docker (`DockerConstants`) no `FurLab.Core`, acessíveis tanto pelo CLI quanto por futuros frontends.

#### Cenário: Acesso a constantes de container
- **QUANDO** qualquer consumidor (CLI ou frontend) precisa referenciar o nome padrão do container, imagem ou configurações de locale
- **ENTÃO** as constantes DEVEM estar disponíveis via `DockerConstants` no namespace `FurLab.Core`
