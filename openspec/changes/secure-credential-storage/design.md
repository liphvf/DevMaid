## Contexto

As senhas dos servidores PostgreSQL são atualmente armazenadas em texto puro no campo `Password` de `ServerConfigEntry`, serializado diretamente no `furlab.jsonc` (`%LocalAppData%\FurLab\furlab.jsonc`). O código inclusive documenta isso explicitamente: `"stored in plain text for dev environments"`.

O projeto usa `net10.0`, já possui `Microsoft.Extensions.DependencyInjection` e `Microsoft.Extensions.Hosting` no `FurLab.Core`, e existe uma interface `IUserConfigService` estabelecida para acesso à configuração. Há intenção declarada de suporte crossplataforma (Linux/macOS) no futuro.

A CLI já possui fluxo de input mascarado via `PostgresPasswordHandler` com `SecureString`, mas a senha convertida para `string` é imediatamente salva em plaintext.

## Objetivos / Não-Objetivos

**Objetivos:**
- Encriptar senhas em repouso usando `Microsoft.AspNetCore.DataProtection`
- Remover o campo `password` em texto puro do `furlab.jsonc`
- Adicionar subcomando `fur settings db-servers set-password` para definir/redefinir senhas
- Degradar graciosamente (prompt interativo) quando senha não está disponível em runtime
- Tornar o modo interativo o padrão em `add`, `rm` e `test` quando argumentos insuficientes
- Extrair helper `SelectServer()` compartilhado por `rm`, `test` e `set-password`

**Não-Objetivos:**
- Migração automática de senhas em plaintext existentes
- Suporte a key stores externos (Azure Key Vault, certificados) — fica para evolução futura
- Encriptação de outros campos de configuração (host, username, etc.)
- Implementar `fur settings db-servers update` (não existe hoje, fora de escopo)

## Decisões

### D1 — Microsoft.AspNetCore.DataProtection como mecanismo de encriptação

**Escolhido sobre:** DPAPI direto (`ProtectedData.Protect`) e Windows Credential Manager.

**Razão:** O DataProtection é a única opção com suporte crossplataforma real. No Windows usa DPAPI automaticamente para proteger as chaves; no Linux/macOS usa AES-256-CBC + HMACSHA256 sem dependência de API do OS. O projeto já possui `IServiceCollection` configurado — a integração é uma linha (`services.AddDataProtection()`). DPAPI direto funcionaria hoje mas quebraria na expansão crossplataforma. Credential Manager exigiria P/Invoke e não resolveria Linux/macOS.

### D2 — Blob encriptado armazenado no próprio furlab.jsonc

**Escolhido sobre:** armazenar fora do JSON (ex: Credential Manager do Windows).

**Razão:** Mantém `furlab.jsonc` como fonte única de verdade para configuração de servidores. Backup, versionamento e inspeção manual continuam possíveis com um único arquivo. O blob base64 é opaco e sem valor sem as chaves — a segurança não depende do local do arquivo.

### D3 — Key ring em %LocalAppData%\FurLab\keys\

**Razão:** Coloca chaves e dados na mesma pasta raiz (`%LocalAppData%\FurLab\`). Backup da pasta = backup completo. As chaves são encriptadas pelo DPAPI do perfil do usuário Windows automaticamente. Configuração via `.PersistKeysToFileSystem()` + `.SetApplicationName("FurLab")`.

### D4 — ICredentialService como abstração sobre DataProtection

**Razão:** Isola a dependência do DataProtection em um único serviço. `QueryCommand`, `SettingsCommand` e qualquer futuro consumidor dependem de `ICredentialService`, não diretamente do `IDataProtector`. Facilita testes (mock da interface) e troca futura do mecanismo.

```
ICredentialService
├── Encrypt(plaintext: string) → string     (base64 blob)
└── TryDecrypt(encrypted: string?) → string? (null se falhar)
```

`TryDecrypt` retorna `null` em vez de lançar exceção — o chamador decide o que fazer (prompt, erro, etc.).

### D5 — Fallback para prompt interativo em runtime (sem re-salvar)

**Razão:** Se `TryDecrypt` retorna `null` (chaves perdidas, campo vazio, primeiro uso), o `QueryCommand` pede a senha ao usuário interativamente via `PostgresPasswordHandler` existente. A senha não é re-salva automaticamente — o usuário deve usar `set-password` explicitamente. Isso evita comportamento surpresa e mantém o controle com o usuário.

### D6 — Modo interativo automático por detecção de ausência de argumentos

**Razão:** Remove a fricção da flag `-i` sem quebrar o modo direto com flags. A lógica é simples: se `--name` ou `--host` ausentes no `add`, entra no interativo; se `--name` ausente no `rm` e `test`, usa `SelectServer()`. O `rm` já fazia isso implicitamente — agora é o padrão explícito para todos.

### D7 — Helper SelectServer() compartilhado

```
private static string? SelectServer(string prompt)
  → busca servidores via UserConfigService
  → se vazio: avisa, retorna null
  → SelectionPrompt<string> com lista de nomes
  → retorna nome selecionado
```

Usado por `rm`, `test` e `set-password`. Elimina triplicação da lógica de seleção e do guard de lista vazia.

### D8 — Senha removida do wizard add; ação "set-password" no menu final

**Razão:** Separa responsabilidades — `add` cuida da configuração do servidor, `set-password` cuida da credencial. No menu final do wizard, a opção "Salvar e definir senha" chama o mesmo fluxo de `set-password` internamente, preservando a UX fluida de configurar tudo de uma vez.

## Riscos / Trade-offs

**[Perda do /keys/]** → Se o diretório de chaves for deletado acidentalmente, todas as `encryptedPassword` tornam-se ilegíveis. Mitigação: documentar que `%LocalAppData%\FurLab\` deve ser incluído em backups; fallback para prompt interativo minimiza o impacto operacional imediato.

**[Reinstalação do Windows / novo perfil]** → As chaves em `/keys/` são encriptadas com DPAPI do perfil atual. Novo perfil = chaves ilegíveis. Mitigação: mesmo tratamento do risco anterior — usuário re-cadastra senhas via `set-password`. Aceitável para uma CLI de desenvolvimento.

**[Breaking change no furlab.jsonc]** → Campo `password` removido; usuários existentes perdem as senhas cadastradas silenciosamente. Mitigação: não há migração automática (fora de escopo); usuário deve rodar `fur settings db-servers set-password` após atualizar. Deve ser comunicado claramente nas release notes.

**[Dois artefatos para fazer backup]** → Antes: só `furlab.jsonc`. Depois: `furlab.jsonc` + `keys/`. Trade-off aceito pela segurança adicionada; mitigado por manter tudo dentro de `%LocalAppData%\FurLab\`.

**[DataProtection em CLI não-ASP.NET]** → A biblioteca é projetada para web mas funciona em qualquer host com `IServiceCollection`. Não há side effects conhecidos. O overhead de inicialização é mínimo.

## Plano de Migração

1. Após atualizar para a nova versão, `furlab.jsonc` existente continua funcionando — mas o campo `password` é ignorado na desserialização (campo removido do modelo)
2. Ao executar qualquer comando que precise de senha, o fallback para prompt interativo é acionado automaticamente
3. Usuário define senhas permanentes via `fur settings db-servers set-password <name>`
4. Não há rollback automático — downgrade requereria re-adicionar servidores com a versão antiga

## Questões em Aberto

- Nenhuma. Todas as decisões de design foram resolvidas durante a fase de exploração.
