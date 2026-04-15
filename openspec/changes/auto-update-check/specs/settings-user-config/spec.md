## MODIFICADO Requisitos

### Requisito: Estrutura do furlab.jsonc
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
  },
  "updateCheck": {
    "channel": "string",          // "winget" | "dotnet" | null
    "lastChecked": "string",      // ISO date (YYYY-MM-DD) ou null
    "latestKnown": "string",      // última versão disponível detectada ou null
    "notified": bool              // true se usuário já foi notificado desta versão
  }
}
```

#### Cenário: Arquivo criado automaticamente
- **QUANDO** sistema acessa configurações e o arquivo não existe
- **ENTÃO** sistema cria `%LocalAppData%\FurLab\furlab.jsonc` com estrutura vazia
- **E** campo `updateCheck` é inicializado como `{ channel: null, lastChecked: null, latestKnown: null, notified: false }`

#### Cenário: Arquivo lido com sucesso
- **QUANDO** sistema lê `furlab.jsonc` existente
- **ENTÃO** sistema parseia JSONC (suportando comentários)
- **E** carrega configurações de servidores, defaults e updateCheck
- **E** se `updateCheck` estiver ausente, inicializa com valores padrão sem erro

#### Cenário: Arquivo com sintaxe inválida
- **QUANDO** `furlab.jsonc` contém sintaxe JSON inválida
- **ENTÃO** sistema exibe mensagem de erro com linha e coluna do erro
- **E** sistema encerra com exit code 1
