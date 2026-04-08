## Requisitos ADICIONADOS

### Requisito: Configurar modelo padrão via CLI
O sistema DEVE permitir ao usuário definir o campo `"model"` no arquivo de configuração do OpenCode através do subcomando `devmaid opencode settings default-model`, tanto em escopo global quanto local.

#### Cenário: Definir modelo com argumento direto em escopo local
- **QUANDO** o usuário executa `devmaid opencode settings default-model <model-id>` sem a flag `--global` e o modelo informado existe na lista de modelos disponíveis
- **ENTÃO** o sistema DEVE localizar o arquivo de configuração local (`opencode.jsonc` ou `opencode.json`) no diretório atual, atualizar o campo `"model"` com o valor informado e exibir o caminho do arquivo alterado

#### Cenário: Definir modelo com argumento direto em escopo global
- **QUANDO** o usuário executa `devmaid opencode settings default-model <model-id> --global` e o modelo informado existe na lista de modelos disponíveis
- **ENTÃO** o sistema DEVE alterar o campo `"model"` em `~/.config/opencode/opencode.jsonc` e exibir o caminho do arquivo alterado

#### Cenário: Definir modelo via menu interativo
- **QUANDO** o usuário executa `devmaid opencode settings default-model` sem informar `<model-id>`
- **ENTÃO** o sistema DEVE executar `opencode models`, exibir um menu interativo com a lista de modelos disponíveis e, após a seleção do usuário, aplicar o modelo escolhido ao arquivo de configuração correspondente ao escopo

#### Cenário: Cancelar seleção no menu interativo
- **QUANDO** o usuário pressiona Esc ou Ctrl+C durante o menu interativo
- **ENTÃO** o sistema DEVE encerrar sem alterar nenhum arquivo de configuração

### Requisito: Resolução de arquivo de configuração
O sistema DEVE determinar qual arquivo de configuração alterar seguindo uma ordem de prioridade, tanto para escopo local quanto global.

#### Cenário: Arquivo .jsonc existe no escopo local
- **QUANDO** existe um arquivo `opencode.jsonc` no diretório atual e o escopo é local
- **ENTÃO** o sistema DEVE alterar esse arquivo

#### Cenário: Apenas .json existe no escopo local
- **QUANDO** não existe `opencode.jsonc` mas existe `opencode.json` no diretório atual e o escopo é local
- **ENTÃO** o sistema DEVE alterar o arquivo `opencode.json`

#### Cenário: Nenhum arquivo existe no escopo local
- **QUANDO** não existe `opencode.jsonc` nem `opencode.json` no diretório atual e o escopo é local
- **ENTÃO** o sistema DEVE criar o arquivo `opencode.jsonc` com o campo `"model"` definido

#### Cenário: Arquivo global existe
- **QUANDO** existe `~/.config/opencode/opencode.jsonc` e o escopo é global
- **ENTÃO** o sistema DEVE alterar esse arquivo preservando todos os demais campos existentes

#### Cenário: Arquivo global não existe
- **QUANDO** não existe `~/.config/opencode/opencode.jsonc` e o escopo é global
- **ENTÃO** o sistema DEVE criar o arquivo com o campo `"model"` definido, criando o diretório pai se necessário

### Requisito: Tolerância a comentários em arquivos .jsonc
O sistema DEVE ser capaz de ler arquivos `.jsonc` que contenham comentários no estilo JSON com comentários (`//` e `/* */`) sem falhar.

#### Cenário: Arquivo .jsonc com comentários é lido corretamente
- **QUANDO** o arquivo de configuração alvo contém comentários
- **ENTÃO** o sistema DEVE ignorar os comentários na leitura, atualizar o campo `"model"` e salvar o arquivo sem erros

### Requisito: Validação do modelo informado
O sistema DEVE validar se o `<model-id>` informado diretamente como argumento existe na lista retornada por `opencode models` antes de gravar no arquivo de configuração.

#### Cenário: Modelo inválido informado como argumento
- **QUANDO** o usuário informa um `<model-id>` que não consta na lista de modelos disponíveis
- **ENTÃO** o sistema DEVE exibir uma mensagem de erro indicando que o modelo não foi encontrado, listar os modelos disponíveis e encerrar com código de saída diferente de zero sem alterar nenhum arquivo

#### Cenário: Modelo válido informado como argumento
- **QUANDO** o usuário informa um `<model-id>` que consta na lista de modelos disponíveis
- **ENTÃO** o sistema DEVE prosseguir normalmente com a gravação no arquivo de configuração

### Requisito: Tratamento de falha ao obter lista de modelos
O sistema DEVE tratar de forma clara a falha ao executar `opencode models`.

#### Cenário: `opencode` não está disponível no PATH
- **QUANDO** o usuário não informa `<model-id>` e o comando `opencode models` falha por `opencode` não estar no PATH
- **ENTÃO** o sistema DEVE exibir uma mensagem de erro clara indicando que o OpenCode não foi encontrado e encerrar com código de saída diferente de zero
