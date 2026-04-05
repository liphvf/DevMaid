# Guia de Início Rápido: devmaid database pgpass

**Feature**: `011-pgpass-cli-setup` | **Data**: 2026-04-04

---

## O que é isto?

O subcomando `devmaid database pgpass` gerencia o arquivo `pgpass.conf` do PostgreSQL no Windows, que permite autenticação sem digitar senha manualmente toda vez que você se conecta a um banco de dados.

**Localização do arquivo**: `C:\Users\<seu-usuario>\AppData\Roaming\postgresql\pgpass.conf`

---

## Pré-requisitos

- Windows com DevMaid instalado (`dotnet tool install devmaid` ou build local)
- Acesso de escrita ao seu diretório `AppData\Roaming` (normalmente disponível sem permissões especiais)

---

## Uso mais comum: adicionar uma entrada

```bash
# Adicionar credencial para banco local (hostname/porta/usuário com padrões)
devmaid database pgpass add meu_banco
# → Solicita senha interativamente

# Adicionar credencial passando a senha diretamente
devmaid database pgpass add meu_banco --password minhasenha

# Adicionar credencial para servidor remoto
devmaid database pgpass add producao --host db.empresa.com --port 5433 --username deploy --password s3cr3t

# Adicionar entrada que vale para qualquer banco no localhost
devmaid database pgpass add "*" --password senhapadrao
```

**Padrões aplicados quando não informados:**

| Parâmetro | Padrão |
|-----------|--------|
| `--host` | `localhost` |
| `--port` | `5432` |
| `--username` | `postgres` |

---

## Verificar entradas salvas

```bash
devmaid database pgpass list
```

Saída de exemplo:
```
HOSTNAME         PORTA  BANCO          USUÁRIO    SENHA
localhost        5432   meu_banco      postgres   ****
db.empresa.com   5433   producao       deploy     ****
```

> As senhas são sempre exibidas como `****` por segurança.

---

## Remover uma entrada

```bash
# Remover com padrões (localhost:5432:meu_banco:postgres)
devmaid database pgpass remove meu_banco

# Remover entrada específica de servidor remoto
devmaid database pgpass remove producao --host db.empresa.com --port 5433 --username deploy
```

---

## Comportamentos importantes

**Duplicatas são ignoradas com segurança:**
```bash
devmaid database pgpass add meu_banco --password abc123
devmaid database pgpass add meu_banco --password abc123
# → "Entrada já existe: localhost:5432:meu_banco:postgres"
# O arquivo não é modificado; código de saída 0
```

**Caracteres especiais na senha são tratados automaticamente:**
```bash
# Senhas com ":" ou "\" são escapadas automaticamente antes de gravar
devmaid database pgpass add meu_banco --password "senha:com:dois:pontos"
```

**Entrada não encontrada ao remover não é um erro:**
```bash
devmaid database pgpass remove banco_inexistente
# → "Entrada não encontrada: localhost:5432:banco_inexistente:postgres"
# Código de saída 0
```

---

## Erros comuns e soluções

| Mensagem | Causa | Solução |
|----------|-------|---------|
| `sem permissão para gravar em %APPDATA%\postgresql\` | Permissão negada | Execute o terminal como Administrador |
| `não foi possível gravar em pgpass.conf — somente-leitura ou em uso` | Arquivo bloqueado | Feche outros processos que possam estar usando o arquivo |
| `a senha não pode ser vazia` | Senha vazia no prompt ou flag | Forneça uma senha válida |
| `formato de host inválido` | Hostname com caracteres inválidos | Verifique o hostname (ex.: sem espaços) |

---

## Fluxo completo de desenvolvimento

```bash
# 1. Configurar acesso ao banco local
devmaid database pgpass add dev_db --password dev123

# 2. Verificar que foi salvo
devmaid database pgpass list

# 3. Agora psql se conecta sem pedir senha:
#    psql -h localhost -U postgres -d dev_db

# 4. Quando não precisar mais
devmaid database pgpass remove dev_db
```

---

## Ajuda inline

```bash
devmaid database pgpass --help
devmaid database pgpass add --help
devmaid database pgpass list --help
devmaid database pgpass remove --help
```
