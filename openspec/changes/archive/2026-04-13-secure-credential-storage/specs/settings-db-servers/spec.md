## ADICIONADO Requirements

### Requirement: settings db-servers set-password
O sistema DEVE expor o subcomando `settings db-servers set-password [name]` para definir ou redefinir a senha encriptada de um servidor cadastrado.

#### Cenário: Com nome do servidor como argumento
- **QUANDO** usuário executa `fur settings db-servers set-password prod`
- **ENTÃO** sistema verifica se o servidor "prod" existe
- **E** solicita a nova senha via input mascarado
- **E** encripta e salva em `ServerConfigEntry.EncryptedPassword`
- **E** exibe: "Senha salva com segurança para 'prod'."

#### Cenário: Sem argumento — seleção interativa
- **QUANDO** usuário executa `fur settings db-servers set-password` sem argumento
- **E** há servidores cadastrados
- **ENTÃO** sistema exibe `SelectionPrompt` com lista de servidores disponíveis
- **E** após seleção, solicita senha via input mascarado
- **E** encripta e salva

#### Cenário: Sem argumento — nenhum servidor cadastrado
- **QUANDO** usuário executa `fur settings db-servers set-password` sem argumento
- **E** não há servidores cadastrados
- **ENTÃO** sistema exibe: "Nenhum servidor configurado. Use `fur settings db-servers add` para adicionar."
- **E** encerra sem erro

#### Cenário: Servidor não encontrado
- **QUANDO** usuário executa `fur settings db-servers set-password nome-inexistente`
- **ENTÃO** sistema exibe: "Servidor 'nome-inexistente' não encontrado."
- **E** encerra com exit code 2

## MODIFICADO Requirements

### Requirement: settings db-servers add
O sistema DEVE expor o subcomando `settings db-servers add` para adicionar um novo servidor ao `furlab.jsonc`.

#### Cenário: Modo interativo automático (sem --name ou --host)
- **QUANDO** usuário executa `fur settings db-servers add` sem fornecer `--name` ou `--host`
- **ENTÃO** sistema entra no modo interativo automaticamente, sem necessidade de flag `-i`
- **E** solicita sequencialmente:
  - Server name (obrigatório)
  - Host (obrigatório)
  - Port (default: 5432)
  - Username (default: postgres)
  - Databases (comma-separated, opcional)
  - Auto-discover databases? [y/N]
  - Se Yes: Exclude patterns (comma-separated, default: template*,postgres)
  - SSL Mode (default: Prefer, seleção de lista)
  - Timeout (default: 30s)
  - Command Timeout (default: 300s)
  - Max Parallelism (default: 4)
- **E** sistema exibe menu final com ações:
  - "Salvar e definir senha"
  - "Salvar e testar conexão"
  - "Salvar sem senha"
  - "Cancelar"

#### Cenário: Modo interativo com pré-preenchimento de flags fornecidas
- **QUANDO** usuário executa `fur settings db-servers add --host db.prod.com --port 5433` sem `--name`
- **ENTÃO** sistema entra no modo interativo
- **E** campos já fornecidos (`--host`, `--port`) aparecem pré-preenchidos como default nos prompts
- **E** usuário pode pressionar Enter para aceitar ou digitar para sobrescrever

#### Cenário: Modo direto com flags completas
- **QUANDO** usuário executa `fur settings db-servers add --name prod --host db.example.com`
- **ENTÃO** sistema salva servidor com os parâmetros fornecidos
- **E** usa defaults para parâmetros não especificados
- **E** servidor é salvo sem senha (usuário deve usar `set-password` para defini-la)

#### Cenário: Ação "Salvar e definir senha" no menu final
- **QUANDO** usuário seleciona "Salvar e definir senha" no menu final do wizard
- **ENTÃO** sistema salva o servidor
- **E** solicita senha via input mascarado
- **E** encripta e salva a senha

#### Cenário: Nome duplicado
- **QUANDO** usuário tenta adicionar servidor com nome já existente
- **ENTÃO** sistema exibe mensagem: "Servidor 'nome' já existe. Use outro nome ou remova o existente primeiro."
- **E** sistema encerra com exit code 2

## REMOVIDO Requirements

### Requirement: settings db-servers add — Modo interativo via flag -i
**Razão**: Flag `-i` removida; modo interativo agora é ativado automaticamente pela ausência de `--name` ou `--host`, tornando a flag redundante e desnecessária.
**Migration**: Substituir `fur settings db-servers add -i` por `fur settings db-servers add` (comportamento idêntico).

## MODIFICADO Requirements

### Requirement: settings db-servers rm
O sistema DEVE expor o subcomando `settings db-servers rm` para remover servidores do `furlab.jsonc`.

#### Cenário: Modo interativo automático (sem --name)
- **QUANDO** usuário executa `settings db-servers rm` sem `--name`
- **E** há servidores cadastrados
- **ENTÃO** sistema exibe `SelectionPrompt` com lista de servidores disponíveis (seleção múltipla)
- **E** remove os servidores selecionados

#### Cenário: Modo direto por nome
- **QUANDO** usuário executa `settings db-servers rm --name prod`
- **ENTÃO** sistema remove o servidor com aquele nome
- **E** exibe mensagem de confirmação

#### Cenário: Nenhum servidor cadastrado
- **QUANDO** usuário executa `settings db-servers rm` sem `--name`
- **E** não há servidores cadastrados
- **ENTÃO** sistema exibe mensagem de aviso e encerra sem erro

#### Cenário: Servidor não encontrado
- **QUANDO** usuário tenta remover servidor que não existe
- **ENTÃO** sistema exibe: "Servidor 'nome' não encontrado."
- **E** sistema encerra com exit code 2

### Requirement: settings db-servers test
O sistema DEVE expor o subcomando `settings db-servers test` para testar conexão com um servidor configurado.

#### Cenário: Modo interativo automático (sem --name)
- **QUANDO** usuário executa `settings db-servers test` sem `--name`
- **E** há servidores cadastrados
- **ENTÃO** sistema exibe `SelectionPrompt` com lista de servidores disponíveis
- **E** após seleção, executa o teste de conexão

#### Cenário: Modo direto por nome
- **QUANDO** usuário executa `settings db-servers test --name prod`
- **ENTÃO** sistema testa conexão com o servidor "prod" diretamente

#### Cenário: Nenhum servidor cadastrado
- **QUANDO** usuário executa `settings db-servers test` sem `--name`
- **E** não há servidores cadastrados
- **ENTÃO** sistema exibe mensagem de aviso e encerra sem erro

#### Cenário: Conexão bem-sucedida
- **QUANDO** usuário executa `settings db-servers test` para um servidor válido
- **ENTÃO** sistema exibe: "✓ Conexão com host:porta bem-sucedida"
- **E** lista databases acessíveis

#### Cenário: Falha de conexão
- **QUANDO** servidor está offline ou inacessível
- **ENTÃO** sistema exibe: "✗ Falha na conexão: <mensagem de erro>"
- **E** sistema encerra com exit code 1

#### Cenário: Falha de autenticação
- **QUANDO** credenciais estão incorretas
- **ENTÃO** sistema exibe: "✗ Falha na autenticação: <mensagem de erro>"
- **E** sistema encerra com exit code 1

#### Cenário: Servidor não encontrado
- **QUANDO** usuário testa servidor que não existe
- **ENTÃO** sistema exibe: "Servidor 'nome' não encontrado."
- **E** sistema encerra com exit code 2
