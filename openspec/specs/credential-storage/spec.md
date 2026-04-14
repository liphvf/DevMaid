# Spec: credential-storage

## Purpose

Define como o sistema armazena e recupera senhas de servidores PostgreSQL de forma segura, usando `Microsoft.AspNetCore.DataProtection` com DPAPI no Windows.

## Requirements

### Requirement: Encriptação de senhas em repouso
O sistema DEVE encriptar senhas de servidores usando `Microsoft.AspNetCore.DataProtection` antes de persistir no `furlab.jsonc`, armazenando o resultado como blob base64 no campo `encryptedPassword`.

#### Cenário: Encriptação ao salvar senha
- **QUANDO** usuário define a senha de um servidor via `set-password`
- **ENTÃO** sistema encripta a senha com `IDataProtector.Protect()`
- **E** armazena o blob base64 resultante em `ServerConfigEntry.EncryptedPassword`
- **E** persiste no `furlab.jsonc` sem nenhum valor em texto puro

#### Cenário: Decriptação ao usar senha
- **QUANDO** sistema necessita da senha de um servidor para conectar
- **ENTÃO** sistema chama `ICredentialService.TryDecrypt(encryptedPassword)`
- **E** retorna a senha em texto puro apenas em memória, sem persistir

#### Cenário: Decriptação falha (chaves indisponíveis ou campo vazio)
- **QUANDO** `TryDecrypt` retorna `null` (chaves ausentes, campo vazio ou blob corrompido)
- **ENTÃO** sistema solicita a senha interativamente ao usuário
- **E** usa a senha fornecida apenas para a sessão atual
- **E** não re-salva automaticamente

### Requirement: Gerenciamento do key ring
O sistema DEVE persistir as chaves de criptografia em `%LocalAppData%\FurLab\keys\`, protegidas com DPAPI no Windows.

#### Cenário: Inicialização do key ring
- **QUANDO** sistema inicializa o `IDataProtectionProvider`
- **ENTÃO** chaves são persistidas em `%LocalAppData%\FurLab\keys\`
- **E** chaves são encriptadas automaticamente pelo DPAPI do perfil do usuário Windows
- **E** application name é definido como `"FurLab"` para isolamento

#### Cenário: Rotação automática de chaves
- **QUANDO** chave ativa expira (padrão: 90 dias)
- **ENTÃO** sistema gera nova chave automaticamente
- **E** mantém chaves antigas no diretório para permitir decriptação de dados anteriores
- **E** dados encriptados com chaves antigas continuam decriptáveis
