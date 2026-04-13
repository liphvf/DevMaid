# Spec: settings-db-servers

## Purpose

Define os subcomandos de gerenciamento de servidores de banco de dados: listagem, adição, remoção e teste de conexão, acessíveis via `fur settings db-servers`.

## Requirements

### Requirement: settings db-servers ls
O sistema DEVE expor o subcomando `settings db-servers ls` que lista todos os servidores configurados no `furlab.jsonc` em formato tabular.

#### Cenário: Servidores existentes
- **QUANDO** usuário executa `settings db-servers ls`
- **ENTÃO** sistema exibe tabela com colunas: Nome, Host, Porta, Databases, Auto-DB
- **E** cada linha representa um servidor configurado
- **E** coluna Auto-DB mostra "Yes" se `fetchAllDatabases` é true, "No" caso contrário

#### Cenário: Nenhum servidor configurado
- **QUANDO** usuário executa `settings db-servers ls` e não há servidores
- **ENTÃO** sistema exibe mensagem: "Nenhum servidor configurado. Use fur settings db-servers add para adicionar."

### Requirement: settings db-servers add
O sistema DEVE expor o subcomando `settings db-servers add` para adicionar um novo servidor ao `furlab.jsonc`.

#### Cenário: Modo interativo (-i)
- **QUANDO** usuário executa `settings db-servers add -i`
- **ENTÃO** sistema solicita sequencialmente:
  - Server name (obrigatório)
  - Host (obrigatório)
  - Port (default: 5432)
  - Username (default: postgres)
  - Password (input oculto)
  - Databases (comma-separated, opcional)
  - Auto-discover databases? [y/N]
  - Se Yes: Exclude patterns (comma-separated, default: template*,postgres)
  - SSL Mode (default: Prefer, seleção de lista)
  - Timeout (default: 30s)
  - Command Timeout (default: 300s)
  - Max Parallelism (default: 4)
- **E** sistema pergunta: "Salvar e testar conexão? [Y/n/c]"
- **E** se Y: salva e testa conexão
- **E** se n: salva sem testar
- **E** se c: cancela

#### Cenário: Modo direto com flags
- **QUANDO** usuário executa `settings db-servers add -n nome -h host -p porta -U user -W pass`
- **ENTÃO** sistema salva servidor com os parâmetros fornecidos
- **E** usa defaults para parâmetros não especificados

#### Cenário: Nome duplicado
- **QUANDO** usuário tenta adicionar servidor com nome já existente
- **ENTÃO** sistema exibe mensagem: "Servidor 'nome' já existe. Use outro nome ou remova o existente primeiro."
- **E** sistema encerra com exit code 2

### Requirement: settings db-servers rm
O sistema DEVE expor o subcomando `settings db-servers rm` para remover servidores do `furlab.jsonc`.

#### Cenário: Modo interativo
- **QUANDO** usuário executa `settings db-servers rm`
- **ENTÃO** sistema exibe lista numerada de servidores
- **E** usuário seleciona um ou mais servidores para remover
- **E** sistema pergunta "Confirmar remoção? [y/N]"
- **E** se confirmado, remove servidores e salva

#### Cenário: Modo direto por nome
- **QUANDO** usuário executa `settings db-servers rm -n nome`
- **ENTÃO** sistema remove o servidor com aquele nome
- **E** sistema exibe mensagem de confirmação

#### Cenário: Servidor não encontrado
- **QUANDO** usuário tenta remover servidor que não existe
- **ENTÃO** sistema exibe mensagem: "Servidor 'nome' não encontrado."
- **E** sistema encerra com exit code 2

### Requirement: settings db-servers test
O sistema DEVE expor o subcomando `settings db-servers test -n <nome>` para testar conexão com um servidor configurado.

#### Cenário: Conexão bem-sucedida
- **QUANDO** usuário executa `settings db-servers test -n localhost`
- **ENTÃO** sistema tenta conectar ao servidor
- **E** se conexão e autenticação funcionam, exibe: "✓ Conexão com localhost:5432 bem-sucedida"
- **E** lista databases acessíveis: "✓ Databases encontradas: db1, db2, db3"

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
