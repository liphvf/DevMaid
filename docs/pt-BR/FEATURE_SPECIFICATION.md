# Documentação de Especificação de Funcionalidades

## Visão Geral do Produto

DevMaid é uma ferramenta CLI baseada em .NET projetada para automatizar tarefas comuns de desenvolvimento. Ela fornece uma interface unificada para operações de banco de dados, gerenciamento de arquivos, instalação de ferramentas de IA e gerenciamento de pacotes Windows.

A ferramenta oferece dois modos de operação:
1. **Modo CLI**: Execução direta via linha de comando
2. **Modo TUI**: Interface de terminal interativa com menus

## Lista de Funcionalidades

### Funcionalidades Principais

1. **Table Parser**
2. **Combine**
3. **Integração com Claude Code**
4. **Integração com OpenCode**
5. **Gerenciador de Pacotes Winget**
6. **Modo TUI Interativo**

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

## Funcionalidade 2: Combine

### Objetivo

Combinar múltiplos arquivos em um único arquivo de saída.

### Descrição Detalhada

O recurso Combine pega múltiplos arquivos de entrada que correspondem a um padrão e os combina em um único arquivo de saída. Isso é útil para consolidar arquivos SQL, arquivos de log ou qualquer arquivo de texto.

### Fluxo de Uso

```bash
# Combinar todos os arquivos SQL em um diretório
devmaid combine -i "C:\temp\*.sql" -o "C:\temp\resultado.sql"

# Combinar com nome de saída padrão
devmaid combine -i "C:\temp\*.txt"
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

## Funcionalidade 3: Integração com Claude Code

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

## Funcionalidade 4: Integração com OpenCode

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

## Funcionalidade 5: Gerenciador de Pacotes Winget

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

## Funcionalidade 6: Modo TUI Interativo

### Objetivo

Fornecer uma interface de terminal amigável para navegar e executar comandos do DevMaid.

### Descrição Detalhada

O modo TUI oferece uma interface interativa baseada em menus que torna o DevMaid acessível para usuários que preferem não lembrar comandos CLI.

### Fluxo de Uso

```bash
devmaid tui
```

1. Menu principal é exibido com comandos disponíveis
2. Usuário navega usando teclas de seta
3. Usuário seleciona um item de menu com Enter
4. Sub-menu ou execução de comando ocorre
5. Progresso é mostrado em tempo real
6. Diálogo de saída exibe resultados
7. Usuário retorna ao menu ou sai

### Estrutura de Menu

```
DevMaid - Interface de Terminal
├── Table Parser
│   ├── Parse CSV para Markdown
│   ├── Parse CSV para JSON
│   ├── Parse Markdown para CSV
│   └── Voltar
├── Utilitários de Arquivo
│   ├── Buscar Arquivos
│   ├── Organizar por Extensão
│   ├── Encontrar Duplicatas
│   └── Voltar
├── Claude Code
│   ├── Instalar Claude Code
│   ├── Verificar Status
│   ├── Configurar
│   └── Voltar
├── OpenCode
│   ├── Instalar OpenCode
│   ├── Verificar Status
│   ├── Configurar
│   └── Voltar
├── Winget
│   ├── Backup de Pacotes
│   ├── Restaurar Pacotes
│   └── Voltar
└── Sair
```

### Navegação por Teclado

| Tecla | Ação |
|-------|------|
| ↑ / ↓ | Navegar itens do menu |
| Enter | Executar item selecionado |
| Esc | Voltar / Sair |

### Suporte a Tema

O TUI detecta automaticamente o tema do terminal:
- **Terminal escuro**: Fundo preto, texto branco/cinza
- **Terminal claro**: Fundo branco, texto preto

### Saída em Tempo Real

Comandos executam de forma assíncrona com exibição de saída em tempo real:
- Diálogo de progresso mostra durante execução
- Saída flui para o diálogo conforme recebida
- Código de saída exibido após conclusão

### Casos Extremos e Tratamento de Erros

| Cenário | Tratamento |
|---------|------------|
| Terminal muito pequeno | Mostrar aviso de tamanho mínimo |
| Comando não encontrado | Exibir erro no diálogo de saída |
| Comando falha | Exibir mensagem de erro com código de saída |
| Comando de longa duração | Mostrar progresso, permitir cancelamento |

---

## Casos de Uso Principais

### Caso de Uso 1: Configuração de Novo Desenvolvedor

**Cenário:** Desenvolvedor obtém uma nova máquina Windows e deseja configurar seu ambiente de desenvolvimento.

**Fluxo:**
1. Instalar DevMaid via dotnet tool
2. Executar `devmaid tui`
3. Usar Winget Backup para restaurar pacotes da máquina antiga
4. Instalar Claude Code via menu
5. Instalar OpenCode via menu

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
- [ ] Adicionar visualização de arquivo no TUI
- [ ] Adicionar histórico de comandos no TUI
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
| `devmaid file search` | - | Buscar arquivos |
| `devmaid file organize` | - | Organizar por extensão |
| `devmaid file duplicates` | - | Encontrar duplicatas |
| `devmaid claude` | - | Comandos do Claude Code |
| `devmaid claude install` | - | Instalar Claude Code |
| `devmaid claude status` | - | Verificar status do Claude |
| `devmaid claude config` | - | Configurar Claude |
| `devmaid opencode` | - | Comandos do OpenCode |
| `devmaid winget` | - | Comandos do Winget |
| `devmaid winget backup` | - | Backup de pacotes |
| `devmaid winget restore` | - | Restaurar pacotes |
| `devmaid tui` | - | Iniciar TUI |

---

## Glossário

| Termo | Definição |
|-------|-----------|
| CLI | Interface de Linha de Comando |
| TUI | Interface de Usuário de Terminal |
| Winget | Gerenciador de Pacotes Windows |
| MCP | Protocolo de Contexto de Modelo |
| DTO | Objeto de Transferência de Dados |
| PostgreSQL | Banco de dados relacional de código aberto |
