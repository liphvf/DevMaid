# Spec: settings-user-config

## Purpose

Define o armazenamento e estrutura do arquivo de configuração do usuário `furlab.jsonc`, incluindo suporte a migração de `appsettings.json` e operações CRUD no ConfigurationService.

## Requirements

### Requirement: Armazenamento em furlab.jsonc
O sistema DEVE armazenar configurações de servidores e defaults do usuário em `%LocalAppData%\FurLab\furlab.jsonc`.

#### Cenário: Arquivo criado automaticamente
- **QUANDO** sistema acessa configurações e o arquivo não existe
- **ENTÃO** sistema cria `%LocalAppData%\FurLab\furlab.jsonc` com estrutura vazia

#### Cenário: Arquivo lido com sucesso
- **QUANDO** sistema lê `furlab.jsonc` existente
- **ENTÃO** sistema parseia JSONC (suportando comentários)
- **E** carrega configurações de servidores e defaults

#### Cenário: Arquivo com sintaxe inválida
- **QUANDO** `furlab.jsonc` contém sintaxe JSON inválida
- **ENTÃO** sistema exibe mensagem de erro com linha e coluna do erro
- **E** sistema encerra com exit code 1

### Requirement: Estrutura do furlab.jsonc
O sistema DEVE usar a seguinte estrutura para o arquivo de configuração:

```jsonc
{
  "servers": [
    {
      "name": "string",           // obrigatório, identificador único
      "host": "string",           // obrigatório
      "port": number,             // obrigatório, default 5432
      "username": "string",       // obrigatório, default postgres
      "password": "string",       // opcional, texto puro
      "databases": ["string"],    // opcional, lista de databases específicas
      "fetchAllDatabases": bool,  // opcional, default false
      "excludePatterns": ["string"], // opcional, usado com fetchAllDatabases
      "sslMode": "string",        // opcional, default Prefer
      "timeout": number,          // opcional, default 30
      "commandTimeout": number,   // opcional, default 300
      "maxParallelism": number    // opcional, default 4
    }
  ],
  "defaults": {
    "outputFormat": "string",     // default csv
    "outputDirectory": "string",  // default ./results
    "fetchAllDatabases": bool,    // default false
    "requireConfirmation": bool,  // default true
    "maxParallelism": number      // default 4
  }
}
```

#### Cenário: Validação de schema
- **QUANDO** sistema lê furlab.jsonc
- **ENTÃO** valida campos obrigatórios de cada servidor (name, host, port, username)
- **E** aplica defaults para campos opcionais não especificados
- **E** rejeita servidores com campos obrigatórios faltando

#### Cenário: Nome de servidor único
- **QUANDO** sistema valida servidores
- **ENTÃO** verifica que todos os nomes são únicos
- **E** rejeita configuração com nomes duplicados

### Requirement: Migração de appsettings.json
O sistema DEVE suportar leitura de `appsettings.json` como fallback durante período de transição.

#### Cenário: furlab.jsonc não existe mas appsettings.json existe
- **QUANDO** sistema não encontra `furlab.jsonc` mas encontra `appsettings.json` no diretório do projeto
- **ENTÃO** sistema lê configurações de `appsettings.json`
- **E** exibe warning: "Configurações encontradas em appsettings.json. Considere migrar para furlab.jsonc usando fur settings db-servers migrate."

#### Cenário: Ambos os arquivos existem
- **QUANDO** ambos `furlab.jsonc` e `appsettings.json` existem
- **ENTÃO** sistema usa apenas `furlab.jsonc`
- **E** ignora `appsettings.json` silenciosamente

### Requirement: Operações CRUD no ConfigurationService
O sistema DEVE fornecer métodos no ConfigurationService para manipular servidores no `furlab.jsonc`.

#### Cenário: Adicionar servidor
- **QUANDO** `AddServer(serverConfig)` é chamado
- **ENTÃO** servidor é adicionado à lista `servers` no JSONC
- **E** arquivo é salvo em disco

#### Cenário: Remover servidor
- **QUANDO** `RemoveServer(serverName)` é chamado
- **ENTÃO** servidor com aquele nome é removido da lista
- **E** arquivo é salvo em disco

#### Cenário: Listar servidores
- **QUANDO** `GetServers()` é chamado
- **ENTÃO** retorna lista de todos os servidores configurados

#### Cenário: Obter servidor por nome
- **QUANDO** `GetServer(serverName)` é chamado
- **ENTÃO** retorna servidor com aquele nome ou null se não encontrado

#### Cenário: Atualizar servidor
- **QUANDO** `UpdateServer(serverConfig)` é chamado
- **ENTÃO** servidor existente é atualizado com novos valores
- **E** arquivo é salvo em disco
