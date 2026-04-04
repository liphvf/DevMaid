# Contrato da Interface CLI: devmaid database pgpass

**Feature**: `011-pgpass-cli-setup` | **Data**: 2026-04-04  
**Padrão de comando**: `devmaid database pgpass <subcomando> [opções]`

---

## Visão Geral

O subcomando `devmaid database pgpass` gerencia o arquivo `%APPDATA%\postgresql\pgpass.conf` no Windows, permitindo autenticação PostgreSQL sem senha de forma persistente. É aninhado dentro do grupo `database` por ser uma operação de configuração de banco de dados.

```
devmaid database pgpass <subcomando>

Subcomandos:
  add     Adiciona uma nova entrada de credencial PostgreSQL ao pgpass.conf
  list    Lista todas as entradas existentes (senhas mascaradas)
  remove  Remove uma entrada específica do pgpass.conf
```

**Hierarquia completa de comandos:**
```
devmaid
└── database
    ├── backup
    ├── restore
    └── pgpass          ← este feature
        ├── add
        ├── list
        └── remove
```

---

## Subcomando: `database pgpass add`

### Assinatura

```
devmaid database pgpass add <banco> [opções]
```

### Argumentos Posicionais

| Argumento | Tipo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `<banco>` | `string` | Sim | Nome do banco de dados (ou `*` para curinga) |

### Opções

| Opção | Alias | Tipo | Obrigatório | Padrão | Descrição |
|-------|-------|------|-------------|--------|-----------|
| `--password` | `-W` | `string` | Não* | — | Senha PostgreSQL. Se omitida, solicitada interativamente. |
| `--host` | `-h` | `string` | Não | `localhost` | Hostname ou endereço IP do servidor PostgreSQL |
| `--port` | `-p` | `string` | Não | `5432` | Porta TCP do servidor PostgreSQL |
| `--username` | `-U` | `string` | Não | `postgres` | Nome de usuário PostgreSQL |

> \* A senha é sempre necessária, mas pode ser fornecida via prompt interativo em vez de flag.

### Comportamento

1. Valida `<banco>` (não pode ser vazio)
2. Se `--password` omitido: solicita interativamente (`Senha: `) com mascaramento
3. Valida `--host` via `SecurityUtils.IsValidHost()` se fornecido explicitamente
4. Valida `--port` via `SecurityUtils.IsValidPort()` se fornecida explicitamente
5. Verifica duplicata por chave `(host, porta, banco, usuario)` — se existe, informa e sai com código `0`
6. Cria `%APPDATA%\postgresql\` se não existir
7. Escapa `:` → `\:` e `\` → `\\` na senha
8. Anexa linha `hostname:porta:banco:usuario:senha` ao final do arquivo
9. Exibe mensagem de sucesso em stdout

### Exemplos

```bash
# Mínimo (banco obrigatório; senha solicitada interativamente)
devmaid database pgpass add meu_banco

# Banco + senha via flag
devmaid database pgpass add meu_banco --password minhasenha

# Todos os parâmetros explícitos
devmaid database pgpass add producao --host db.empresa.com --port 5433 --username deploy --password s3cr3t

# Curinga no banco (qualquer banco no localhost com postgres)
devmaid database pgpass add "*" --password senhapadrao
```

### Saídas

| Situação | Mensagem (stdout/stderr) | Código de Saída |
|----------|--------------------------|-----------------|
| Sucesso | `Entrada adicionada: localhost:5432:meu_banco:postgres` | `0` |
| Duplicata | `Entrada já existe: localhost:5432:meu_banco:postgres` | `0` |
| Banco vazio | `Erro: o argumento <banco> é obrigatório.` (stderr) | `2` |
| Senha vazia após prompt | `Erro: a senha não pode ser vazia.` (stderr) | `2` |
| Host inválido | `Erro: formato de host inválido: "<valor>".` (stderr) | `2` |
| Porta inválida | `Erro: porta deve ser um número entre 1 e 65535.` (stderr) | `2` |
| Permissão negada | `Erro: sem permissão para gravar em %APPDATA%\postgresql\. Execute o comando em um terminal com privilégios de administrador.` (stderr) | `1` |
| Arquivo travado | `Erro: não foi possível gravar em pgpass.conf — o arquivo pode estar somente-leitura ou em uso por outro processo.` (stderr) | `1` |

---

## Subcomando: `database pgpass list`

### Assinatura

```
devmaid database pgpass list
```

### Argumentos / Opções

Nenhum.

### Comportamento

1. Verifica se `%APPDATA%\postgresql\pgpass.conf` existe
2. Se não existe ou está vazio: exibe mensagem informativa e sai com código `0`
3. Lê todas as entradas (ignorando linhas de comentário)
4. Exibe em formato tabular com senhas substituídas por `****`

### Formato de Saída

```
HOSTNAME         PORTA  BANCO          USUÁRIO    SENHA
localhost        5432   meu_banco      postgres   ****
db.empresa.com   5433   producao       deploy     ****
localhost        5432   *              postgres   ****
```

### Exemplos

```bash
devmaid database pgpass list
```

### Saídas

| Situação | Mensagem (stdout) | Código de Saída |
|----------|-------------------|-----------------|
| Com entradas | Tabela formatada (senhas mascaradas) | `0` |
| Arquivo vazio / inexistente | `Nenhuma entrada configurada em pgpass.conf.` | `0` |

---

## Subcomando: `database pgpass remove`

### Assinatura

```
devmaid database pgpass remove <banco> [opções]
```

### Argumentos Posicionais

| Argumento | Tipo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `<banco>` | `string` | Sim | Nome do banco de dados da entrada a remover (ou `*`) |

### Opções

| Opção | Alias | Tipo | Obrigatório | Padrão | Descrição |
|-------|-------|------|-------------|--------|-----------|
| `--host` | `-h` | `string` | Não | `localhost` | Hostname da entrada a remover |
| `--port` | `-p` | `string` | Não | `5432` | Porta da entrada a remover |
| `--username` | `-U` | `string` | Não | `postgres` | Usuário da entrada a remover |

### Comportamento

1. Valida `<banco>` (não pode ser vazio)
2. Aplica padrões para `--host`, `--port`, `--username` não fornecidos
3. Lê todas as entradas do arquivo
4. Filtra a entrada com chave `(host, porta, banco, usuario)` correspondente
5. Se não encontrada: exibe mensagem informativa, arquivo inalterado, sai com código `0`
6. Se encontrada: reescreve o arquivo sem a entrada removida; exibe mensagem de sucesso

### Exemplos

```bash
# Remove entrada com padrões
devmaid database pgpass remove meu_banco

# Remove entrada específica
devmaid database pgpass remove producao --host db.empresa.com --port 5433 --username deploy
```

### Saídas

| Situação | Mensagem (stdout/stderr) | Código de Saída |
|----------|--------------------------|-----------------|
| Sucesso | `Entrada removida: localhost:5432:meu_banco:postgres` | `0` |
| Não encontrada | `Entrada não encontrada: localhost:5432:meu_banco:postgres` | `0` |
| Banco vazio | `Erro: o argumento <banco> é obrigatório.` (stderr) | `2` |
| Arquivo travado | `Erro: não foi possível gravar em pgpass.conf — o arquivo pode estar somente-leitura ou em uso por outro processo.` (stderr) | `1` |

---

## Integração com `DatabaseCommand`

`DatabaseCommand.Build()` é modificado para incluir `PgPassCommand.Build()` como subcomando:

```csharp
public static Command Build()
{
    var command = new Command("database", "Database utilities.")
    {
        BuildBackupCommand(),
        BuildRestoreCommand(),
        PgPassCommand.Build()   // ← adicionado por esta feature
    };
    return command;
}
```

Nenhuma alteração em `Program.cs` é necessária — `DatabaseCommand` já está registrado no `rootCommand`.

---

## Códigos de Saída (Resumo)

| Código | Significado |
|--------|-------------|
| `0` | Sucesso (incluindo duplicata ignorada, entrada não encontrada) |
| `1` | Erro de I/O ou permissão |
| `2` | Argumento inválido ou ausente |

---

## Restrições de Segurança

- A senha **nunca** aparece em logs, rastreamentos de pilha ou texto de ajuda
- A senha **nunca** é exibida na listagem (sempre `****`)
- A senha **nunca** é exibida no eco do terminal durante prompt interativo
- O arquivo pgpass não deve ser criado com permissões menos restritivas do que o diretório pai
