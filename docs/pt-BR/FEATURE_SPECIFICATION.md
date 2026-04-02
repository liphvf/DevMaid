# Documentação de Especificação de Funcionalidades

## Visão Geral do Produto

DevMaid é uma ferramenta CLI baseada em .NET projetada para automatizar tarefas comuns de desenvolvimento. Ela fornece uma interface unificada para operações de banco de dados, gerenciamento de arquivos, instalação de ferramentas de IA e gerenciamento de pacotes Windows.

## Lista de Funcionalidades

### Funcionalidades Principais

1. **Table Parser**
2. **Utilitários de Banco de Dados**
3. **Utilitário de Arquivos (Combine)**
4. **Integração com Claude Code**
5. **Integração com OpenCode**
6. **Gerenciador de Pacotes Winget**
7. **Query SQL & Exportação**
8. **Limpeza de Build (.NET Clean)**
9. **Gerenciador de Features Windows**

---

## Funcionalidade 1: Table Parser

### Objetivo

Analisar tabelas de banco de dados PostgreSQL e gerar automaticamente definições de classes C# com propriedades correspondendo às colunas da tabela.

### Descrição Detalhada

O Table Parser conecta-se a um banco de dados PostgreSQL, recupera metadados de uma tabela especificada e gera uma classe C# com propriedades correspondendo às colunas da tabela.

### Fluxo de Uso

```bash
devmaid table-parser -d banco -t usuarios -u postgres -H localhost
```

1. Usuário fornece parâmetros de conexão com banco de dados
2. Ferramenta conecta ao banco de dados PostgreSQL
3. Ferramenta consulta metadados da tabela (nomes de colunas, tipos, anulável)
4. Ferramenta gera classe C# com tipos de propriedade apropriados
5. Saída é salva em arquivo

### Regras de Negócio

- Conexão com banco de dados requer credenciais válidas
- A tabela deve existir no banco de dados especificado
- Tipos de coluna são mapeados para tipos C# equivalentes:
  - `int` → `int`
  - `varchar(n)` → `string`
  - `timestamp` → `DateTime`
  - `boolean` → `bool`
  - `numeric` → `decimal`
  - etc.
- Colunas anuláveis geram propriedades C# anuláveis (`int?`, `string?`, etc.)
- Detecção de chave primária para propriedade Id potencial

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Credenciais de banco inválidas | Exibir mensagem de erro, sair com código 1 |
| Tabela não encontrada | Exibir erro "Tabela não encontrada" |
| Timeout de conexão | Exibir erro de timeout com sugestão de retry |
| Tipo de coluna não suportado | Usar tipo `object` com comentário de aviso |
| Tabela vazia | Gerar classe vazia com comentário |

### Opções

| Opção | Obrigatório | Padrão | Descrição |
|-------|--------------|--------|-----------|
| `-d`, `--database` | Sim | - | Nome do banco de dados |
| `-t`, `--table` | Não | - | Nome da tabela |
| `-u`, `--user` | Não | postgres | Usuário do banco de dados |
| `-p`, `--password` | Não | - | Senha do banco de dados (solicitada se não fornecida) |
| `-H`, `--host` | Não | localhost | Host do banco de dados |
| `-o`, `--output` | Não | ./tabela.class | Caminho do arquivo de saída |

---

## Funcionalidade 2: Utilitários de Banco de Dados

### Objetivo

Fornecer utilitários para backup e restore de bancos de dados PostgreSQL.

### Descrição Detalhada

O comando database fornece funcionalidades para criar backups e restaurar bancos de dados PostgreSQL usando pg_dump e pg_restore.

### Sub-Funcionalidades

#### 2.1 Backup de Banco de Dados

Cria backups de bancos de dados PostgreSQL usando pg_dump.

#### 2.2 Restore de Banco de Dados

Restaura bancos de dados PostgreSQL usando pg_restore a partir de arquivos .dump.

### Fluxo de Uso

```bash
# Backup de um único banco de dados
devmaid database backup meubanco -h localhost -U postgres

# Backup de todos os bancos de dados
devmaid database backup --all -h localhost -U postgres -o "C:\backups"

# Restore de um banco de dados específico
devmaid database restore meubanco "C:\backups\meubanco.dump"

# Restore de todos os bancos de dados de um diretório
devmaid database restore --all "C:\backups"

# Restore de todos os bancos de dados do diretório atual
devmaid database restore --all
```

### Regras de Negócio

- **Backup**:
  - Usa pg_dump para criar backups em formato customizado
  - Suporta backup de um único banco ou todos os bancos
  - Arquivos de backup são criados com extensão .dump
  - Se não especificado, usa diretório atual para saída

- **Restore**:
  - Usa pg_restore para restaurar backups
  - Cria automaticamente o banco de dados se não existir
  - Suporta restore de um único banco ou todos de um diretório
  - Se não especificado, procura arquivos .dump no diretório atual
  - Usa nome do arquivo (sem extensão) como nome do banco de dados

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| pg_dump/pg_restore não encontrado | Exibir erro com instruções de instalação do PostgreSQL |
| Credenciais inválidas | Exibir mensagem de erro, sair com código 1 |
| Arquivo de dump não encontrado | Exibir erro "Arquivo não encontrado" |
| Banco de dados já existe no restore | Exibir aviso, continuar com restore |
| Diretório de restore não encontrado | Exibir erro "Diretório não encontrado" |
| Nenhum arquivo .dump encontrado | Exibir aviso "Nenhum arquivo .dump encontrado" |

### Opções

#### Backup

| Opção | Obrigatório | Padrão | Descrição |
|-------|--------------|--------|-----------|
| `<database>` | Não* | - | Nome do banco de dados (obrigatório sem --all) |
| `-a`, `--all` | Não | false | Backup de todos os bancos |
| `-h`, `--host` | Não | localhost | Host do banco de dados |
| `-p`, `--port` | Não | 5432 | Porta do banco de dados |
| `-U`, `--username` | Não | - | Usuário do banco de dados |
| `-W`, `--password` | Não | - | Senha (solicitada se não fornecida) |
| `-o`, `--output` | Não | diretório atual | Caminho de saída |

#### Restore

| Opção | Obrigatório | Padrão | Descrição |
|-------|--------------|--------|-----------|
| `<database>` | Não* | - | Nome do banco (obrigatório sem --all) |
| `<file>` | Não | `<database>.dump` | Arquivo de dump para restore |
| `-a`, `--all` | Não | false | Restore de todos os bancos |
| `-d`, `--directory` | Não | diretório atual | Diretório com arquivos .dump |
| `-h`, `--host` | Não | localhost | Host do banco de dados |
| `-p`, `--port` | Não | 5432 | Porta do banco de dados |
| `-U`, `--username` | Não | - | Usuário do banco de dados |
| `-W`, `--password` | Não | - | Senha (solicitada se não fornecida) |

---

## Funcionalidade 3: Utilitários de Arquivo (Combine)

### Objetivo

Combinar múltiplos arquivos em um único arquivo de saída.

### Descrição Detalhada

O recurso Combine pega múltiplos arquivos de entrada que correspondem a um padrão e os combina em um único arquivo de saída. Isso é útil para consolidar arquivos SQL, arquivos de log ou qualquer arquivo de texto.

### Fluxo de Uso

```bash
# Combinar todos os arquivos SQL em um diretório
devmaid file combine -i "C:\temp\*.sql" -o "C:\temp\resultado.sql"

# Combinar com nome de saída padrão
devmaid file combine -i "C:\temp\*.txt"
```

### Regras de Negócio

- Entrada deve ser um padrão de arquivo válido (ex: `*.sql`, `*.txt`)
- Arquivo de saída é criado ou sobrescrito
- Se nenhuma saída for especificada, cria `CombineFiles.<extensão>` no mesmo diretório
- Arquivos são processados em ordem alfabética
- Codificação UTF-8 é usada para saída

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Nenhum arquivo corresponde ao padrão | Exibir erro "Arquivos não encontrados" |
| Padrão inválido | Exibir erro "Padrão de entrada inválido" |
| Padrão vazio | Exibir erro "Padrão de entrada é obrigatório" |

---

## Funcionalidade 4: Integração com Claude Code

### Objetivo

Instalar e configurar o CLI do Claude Code para assistência de desenvolvimento.

### Descrição Detalhada

Simplifica a instalação e configuração do assistente de IA Claude Code da Anthropic.

### Sub-Funcionalidades

#### 3.1 Instalar Claude Code

Instala o Claude Code via Gerenciador de Pacotes Windows (winget).

#### 3.2 Configurar MCP de Banco de Dados

Adiciona configuração de ferramenta de banco de dados MCP (Model Context Protocol).

#### 3.3 Configurar Ambiente Windows

Atualiza configurações do Claude para ambiente Windows (shell, permissões).

### Fluxo de Uso

```bash
# Instalar Claude Code
devmaid claude install

# Verificar status
devmaid claude status

# Configurar MCP de banco de dados
devmaid claude settings mcp-database

# Configurar ambiente Windows
devmaid claude settings win-env
```

### Regras de Negócio

- Requer sistema operacional Windows
- Requer winget instalado
- Instalação requer privilégios de administrador (via UAC)
- Modificação configura configurações do Claude nível de usuário

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Winget não instalado | Exibir erro com instruções de instalação |
| Já instalado | Pular instalação, mostrar status |
| Instalação falhou | Exibir erro com código de saída |
| Arquivo de configuração não encontrado | Criar nova configuração |

---

## Funcionalidade 5: Integração com OpenCode

### Objetivo

Instalar e configurar a ferramenta CLI OpenCode.

### Descrição Detalhada

Gerencia instalação e configuração do OpenCode para fluxos de trabalho de desenvolvimento.

### Sub-Funcionalidades

#### 4.1 Instalar OpenCode

Instala o OpenCode via gerenciadores de pacote disponíveis.

#### 4.2 Verificar Status

Verifica instalação e versão do OpenCode.

#### 4.3 Configurar

Define configuração do OpenCode.

### Fluxo de Uso

```bash
# Instalar OpenCode
devmaid opencode install

# Verificar status
devmaid opencode status

# Configurar
devmaid opencode config
```

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Já instalado | Mostrar informações de versão |
| Instalação falhou | Exibir mensagem de erro |
| Não encontrado no PATH | Sugerir atualização do PATH |

---

## Funcionalidade 6: Gerenciador de Pacotes Winget

### Objetivo

Fazer backup e restaurar pacotes Windows instalados via winget.

### Descrição Detalhada

Permite que usuários exportem seus pacotes instalados para um arquivo JSON e os restaurem em máquinas diferentes ou após reinstalação do sistema.

### Sub-Funcionalidades

#### 5.1 Backup de Pacotes

Exporta todos os pacotes winget instalados para um arquivo JSON.

#### 5.2 Restaurar Pacotes

Importa pacotes de um backup criado anteriormente.

### Fluxo de Uso

```bash
# Backup de pacotes
devmaid winget backup -o "C:\backups"

# Restaurar pacotes
devmaid winget restore -i "C:\backups\backup-winget.json"
```

### Regras de Negócio

- Backup cria `backup-winget.json` no diretório especificado
- Restore usa funcionalidade de importação do winget
- Apenas pacotes instalados pelo usuário são salvos (não pacotes do sistema)
- Restore pode requerer confirmação do usuário para instalação de pacotes

### Formato de Saída (backup-winget.json)

```json
{
  "CreationDate": "2024-01-15T10:30:00",
  "Packages": [
    {
      "Id": "Git.Git",
      "Version": "2.43.0"
    },
    {
      "Id": "Microsoft.VisualStudioCode",
      "Version": "1.85.0"
    }
  ]
}
```

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Nenhum pacote instalado | Criar arquivo de backup vazio |
| Arquivo de backup já existe | Solicitar confirmação para sobrescrever |
| Arquivo de restore não encontrado | Exibir erro "Arquivo não encontrado" |
| Pacote não disponível | Pular pacote, continuar com outros |
| Rede indisponível | Exibir erro de rede, permitir retry |

---

## Funcionalidade 7: Query SQL & Exportação

### Objetivo
Executa comandos SQL (script) em diversos lugares e gera uma lista consolidada em CSV automaticamente.

### Descrição Detalhada
Suporta banco de dados único, múltiplos bancos de dados num mesmo localhost ou distribuição em diversos servidores usando configurações do appsettings.json, exportando uma saída formatada e unificada no final do processamento.

### Fluxo de Uso
```bash
devmaid query run --input script.sql --output result.csv -h localhost -d mydb
devmaid query run --all --input script.sql --output ./resultados -h localhost
```

---

## Funcionalidade 8: Limpeza de Build (.NET Clean)

### Objetivo
Libera espaço e resolve possíveis problemas de cache removendo os diretórios de saída bin e obj.

### Fluxo de Uso
```bash
devmaid clean
devmaid clean "C:\MeusProjetos"
```

---

## Funcionalidade 9: Gerenciador de Funcionalidades do Windows

### Objetivo
Permite o backup de Features nativas ativadas no Windows para arquivos JSON, podendo restaurá-las usando o dism.

### Fluxo de Uso
```bash
devmaid windowsfeatures list
devmaid windowsfeatures export "C:\backups\windowsfeatures.json"
devmaid windowsfeatures import "C:\backups\windowsfeatures.json"
```

---

## Casos de Uso Principais

### Caso de Uso 1: Configuração de Novo Desenvolvedor

**Cenário:** Desenvolvedor obtém uma nova máquina Windows e deseja configurar seu ambiente de desenvolvimento.

**Fluxo:**
1. Instalar DevMaid via dotnet tool
2. Executar `devmaid winget restore` para restaurar pacotes da máquina antiga
3. Instalar Claude Code: `devmaid claude install`
4. Instalar OpenCode: `devmaid opencode install`

### Caso de Uso 2: Geração de Classe de Banco de Dados

**Cenário:** Desenvolvedor precisa criar classes C# para tabelas de banco de dados existentes.

**Fluxo:**
1. Executar `devmaid table-parser -d meubanco -t usuarios`
2. Copiar classe gerada para o projeto
3. Modificar conforme necessário

### Caso de Uso 3: Backup de Sistema

**Cenário:** Desenvolvedor deseja fazer backup de aplicativos instalados antes de reinstalar o sistema.

**Fluxo:**
1. Executar `devmaid winget backup -o D:\backups`
2. Armazenar arquivo de backup em local seguro

---

## Roteiro Futuro de Ideias

### Prioridade 1 - Curto Prazo

- [ ] Adicionar suporte para MySQL/SQL Server no Table Parser
- [ ] Adicionar arquivo de configuração para opções padrão

### Prioridade 2 - Médio Prazo

- [ ] Adicionar sistema de plugins para comandos customizados
- [ ] Adicionar sincronização em nuvem para backups winget
- [ ] Adicionar suporte para macOS/Linux
- [ ] Adicionar interface web de configuração

### Prioridade 3 - Longo Prazo

- [ ] Adicionar sugestões de comandos com IA
- [ ] Adicionar recursos de colaboração em equipe
- [ ] Adicionar execução de scripts customizados
- [ ] Adicionar integração com IDEs (VS Code, Visual Studio)

---

## Apêndice: Referência de Comandos

### Referência Rápida

| Comando | Atalho | Descrição |
|---------|--------|-----------|
| `devmaid table-parser` | `tableparser` | Parse de tabela para classe C# |
| `devmaid file` | - | Utilitários de arquivo |
| `devmaid file combine` | - | Combinar arquivos em um |
| `devmaid claude` | - | Comandos do Claude Code |
| `devmaid claude install` | - | Instalar Claude Code |
| `devmaid claude status` | - | Verificar status do Claude |
| `devmaid claude config` | - | Configurar Claude |
| `devmaid opencode` | - | Comandos do OpenCode |
| `devmaid winget` | - | Comandos do Winget |
| `devmaid winget backup` | - | Backup de pacotes |
| `devmaid winget restore` | - | Restaurar pacotes |
| `devmaid database` | - | Comandos de banco de dados |
| `devmaid database backup` | - | Backup de banco de dados |
| `devmaid database restore` | - | Restore de banco de dados |
| `devmaid query run` | - | Consultar multi-database exportando CSV |
| `devmaid clean` | - | Limpar pastas bin e obj de um diretorio |
| `devmaid windowsfeatures` | - | Gerenciar optional Features |

---

## Glossário

| Termo | Definição |
|-------|-----------|
| CLI | Interface de Linha de Comando |
| Winget | Gerenciador de Pacotes Windows |
| MCP | Protocolo de Contexto de Modelo |
| DTO | Objeto de Transferência de Dados |
| PostgreSQL | Banco de dados relacional de código aberto |
