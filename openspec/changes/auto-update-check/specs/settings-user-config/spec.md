## MODIFIED Requirements

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
  },
  "updateCheck": {               // NOVO: configuração de verificação de atualizações
    "enabled": bool,             // default true para winget, false para manual
    "installationMethod": "string",  // "winget", "dotnet-tool", "manual", ou null
    "methodVerifiedAt": "string",    // ISO 8601 date, ou null
    "nextCheckDue": "string",          // ISO 8601 datetime
    "checkInProgress": bool            // default false
  }
}
```

#### Cenário: Validação de schema inclui updateCheck
- **QUANDO** sistema lê furlab.jsonc
- **ENTÃO** valida campos obrigatórios de cada servidor (name, host, port, username)
- **E** aplica defaults para campos opcionais não especificados
- **E** rejeita servidores com campos obrigatórios faltando
- **E** valida que updateCheck, se presente, tem estrutura válida
- **E** aplica defaults para updateCheck.enabled baseado no installationMethod

#### Cenário: updateCheck é opcional e tem defaults
- **QUANDO** sistema lê furlab.jsonc sem a seção updateCheck
- **ENTÃO** sistema cria updateCheck com valores default:
  - enabled: false
  - installationMethod: null
  - methodVerifiedAt: null
  - nextCheckDue: data/hora atual
  - checkInProgress: false
- **E** continua operação normalmente

## ADDED Requirements

### Requirement: UserConfigService suporta operações de UpdateCheck
O sistema DEVE fornecer métodos no UserConfigService para ler e manipular as configurações de updateCheck.

#### Cenário: Obter configuração de update check
- **QUANDO** `GetUpdateCheckConfig()` é chamado
- **ENTÃO** retorna objeto UpdateCheckConfig com valores atuais
- **E** se updateCheck não existe no JSONC, retorna valores default

#### Cenário: Salvar configuração de update check
- **QUANDO** `SaveUpdateCheckConfig(updateCheckConfig)` é chamado
- **ENTÃO** atualiza seção updateCheck no furlab.jsonc
- **E** preserva todas as outras configurações existentes
- **E** salva arquivo em disco

#### Cenário: Atualizar método de instalação
- **QUANDO** `SetInstallationMethod(method, verifiedAt)` é chamado
- **ENTÃO** atualiza updateCheck.installationMethod = method
- **E** atualiza updateCheck.methodVerifiedAt = verifiedAt
- **E** se method == "winget", define updateCheck.enabled = true
- **E** se method == "manual", define updateCheck.enabled = false
- **E** salva arquivo em disco
