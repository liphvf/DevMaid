# Especificação de Feature: Comando CLI para Configurar pgpass

**Branch da Feature**: `011-pgpass-cli-setup`  
**Criado em**: 2026-04-04  
**Status**: Rascunho  
**Entrada**: Descrição do usuário: "Comando CLI para configurar pgpass.conf no Windows para autenticação PostgreSQL sem senha"

## Cenários de Usuário e Testes *(obrigatório)*

### História de Usuário 1 — Configurar Arquivo de Senhas PostgreSQL via CLI (Prioridade: P1)

Um desenvolvedor ou administrador de banco de dados quer configurar autenticação PostgreSQL sem senha no Windows sem precisar localizar manualmente o diretório correto, criar a estrutura de pastas e editar o arquivo de configuração à mão. Ele executa um único comando CLI fornecendo os dados de conexão (host, porta, banco de dados, usuário, senha) e a ferramenta cuida de toda a configuração automaticamente.

**Por que esta prioridade**: Esta é a funcionalidade central — o motivo principal de existência do comando. Sem ela funcionando, nada mais tem valor.

**Teste Independente**: Pode ser testado completamente executando o comando com parâmetros de conexão válidos e verificando que o arquivo `pgpass.conf` foi criado/atualizado no caminho correto do Windows com o formato de entrada correto.

**Cenários de Aceitação**:

1. **Dado que** o diretório `postgresql` não existe em `AppData\Roaming`, **Quando** o usuário executa o comando CLI com parâmetros de conexão válidos, **Então** o diretório é criado automaticamente e o `pgpass.conf` é escrito com a entrada correta
2. **Dado que** o `pgpass.conf` já existe com outras entradas, **Quando** o usuário adiciona uma nova entrada via CLI, **Então** as entradas existentes são preservadas e a nova é adicionada ao final
3. **Dado que** o usuário fornece todos os parâmetros obrigatórios (host, porta, banco, usuário, senha), **Quando** o comando é executado, **Então** a entrada é escrita no formato correto `hostname:porta:banco:usuario:senha`
4. **Dado que** o usuário quer usar curinga para o campo de banco de dados, **Quando** ele omite o parâmetro de banco ou passa `*`, **Então** `*` é escrito como valor do banco na entrada

---

### História de Usuário 2 — Listar Entradas Existentes do pgpass (Prioridade: P2)

Um usuário quer ver quais entradas de senha PostgreSQL já estão armazenadas no seu `pgpass.conf` sem precisar navegar manualmente até a pasta AppData e abrir o arquivo.

**Por que esta prioridade**: Visibilidade sobre a configuração existente ajuda o usuário a evitar duplicatas e entender o estado atual antes de fazer alterações.

**Teste Independente**: Pode ser testado executando o subcomando de listagem após adicionar entradas, verificando que a saída exibe todas as entradas armazenadas em formato legível com as senhas mascaradas por segurança.

**Cenários de Aceitação**:

1. **Dado que** o `pgpass.conf` existe com uma ou mais entradas, **Quando** o usuário executa o comando de listar, **Então** todas as entradas são exibidas com senhas mascaradas (ex.: `****`)
2. **Dado que** o `pgpass.conf` não existe ou está vazio, **Quando** o usuário executa o comando de listar, **Então** uma mensagem clara indica que nenhuma entrada está configurada

---

### História de Usuário 3 — Remover uma Entrada Específica do pgpass (Prioridade: P3)

Um usuário quer remover uma entrada de conexão PostgreSQL específica do `pgpass.conf` quando as credenciais mudam ou a conexão não é mais necessária.

**Por que esta prioridade**: A capacidade de limpeza evita o acúmulo de credenciais desatualizadas ou inseguras, mas não é necessária para o fluxo de configuração principal.

**Teste Independente**: Pode ser testado adicionando uma entrada, removendo-a pela combinação de host/usuário/banco e verificando que ela não aparece mais no `pgpass.conf`.

**Cenários de Aceitação**:

1. **Dado que** uma entrada correspondente existe, **Quando** o usuário executa o comando de remover com host, banco e usuário correspondentes, **Então** a entrada é removida e as demais são preservadas
2. **Dado que** nenhuma entrada correspondente existe, **Quando** o usuário executa o comando de remover, **Então** uma mensagem informativa é exibida e o arquivo permanece inalterado

---

### Casos de Borda

- O que acontece quando o usuário fornece um formato de host inválido ou uma senha vazia?
- Como o sistema lida com uma entrada que é duplicata exata de uma já existente?
- O que acontece quando `AppData\Roaming` não é acessível por falta de permissão?
- Como o sistema lida com caracteres especiais (`:`, `\`, espaços) no campo de senha?
- O que acontece quando o `pgpass.conf` está somente-leitura ou travado por outro processo?

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **RF-001**: A CLI DEVE fornecer um comando para adicionar uma nova entrada ao `pgpass.conf` aceitando host, porta, banco de dados, usuário e senha como parâmetros
- **RF-002**: A CLI DEVE criar automaticamente o diretório `AppData\Roaming\postgresql\` se ele não existir
- **RF-003**: A CLI DEVE escrever entradas no formato correto: `hostname:porta:banco:usuario:senha`
- **RF-004**: A CLI DEVE suportar `*` como valor curinga para o campo de banco de dados, aceitando explicitamente ou como padrão quando o banco for omitido
- **RF-005**: Ao adicionar uma entrada, a CLI DEVE preservar todas as entradas existentes no arquivo e adicionar a nova ao final
- **RF-006**: A CLI DEVE detectar e ignorar entradas duplicadas (mesmo host, porta, banco e usuário) para evitar linhas redundantes
- **RF-007**: A CLI DEVE fornecer um subcomando de listagem que exibe as entradas existentes com senhas mascaradas
- **RF-008**: A CLI DEVE fornecer um subcomando de remoção para excluir entradas que correspondam a um dado host, banco e usuário
- **RF-009**: A CLI DEVE exibir uma mensagem clara de sucesso ou erro para cada operação
- **RF-010**: A CLI DEVE tratar caracteres especiais na senha escapando dois-pontos (`:`) e barras invertidas (`\`) conforme exigido pela especificação do formato pgpass

### Entidades Principais

- **Entrada pgpass**: Representa uma única linha no `pgpass.conf` com os atributos: hostname, porta, banco de dados, usuário, senha
- **Arquivo pgpass.conf**: O arquivo de configuração localizado em `%APPDATA%\postgresql\pgpass.conf` no Windows; contém zero ou mais entradas, uma por linha

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **CS-001**: Um usuário sem conhecimento prévio da localização do arquivo pgpass consegue configurar autenticação PostgreSQL sem senha em menos de 30 segundos usando a CLI
- **CS-002**: 100% das entradas escritas pela CLI são aceitas por clientes PostgreSQL sem correção manual
- **CS-003**: A CLI trata corretamente todos os casos de borda (duplicatas, diretório ausente, caracteres especiais) sem que o usuário precise intervir manualmente
- **CS-004**: Os usuários conseguem visualizar todas as entradas de conexão armazenadas sem precisar abrir ou localizar manualmente o arquivo de configuração

## Premissas

- A ferramenta tem como alvo específico usuários Windows; suporte a pgpass em Linux/macOS está fora do escopo desta feature
- O usuário possui as permissões necessárias no nível do SO para escrever no seu próprio diretório `AppData\Roaming`
- A porta padrão é `5432` quando não fornecida pelo usuário
- O curinga `*` é válido para os campos de host e banco de dados conforme a especificação pgpass, mas a CLI exige um host específico por padrão
- A ferramenta está integrada à CLI existente do DevMaid e segue a estrutura de comandos e convenções de saída do projeto
- Não é necessário fluxo de prompt interativo ou GUI; todos os parâmetros são passados como argumentos de linha de comando (com padrões sensatos quando aplicável)
