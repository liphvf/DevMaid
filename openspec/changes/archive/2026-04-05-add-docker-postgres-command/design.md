## Context

O projeto DevMaid é uma CLI .NET para automação de tarefas de desenvolvimento. O comando será implementado seguindo a arquitetura existente de comandos aninhados (ex: `devmaid git`, `devmaid db`).

### Estado Atual
- CLI com estrutura de comandos hierárquicos via `System.CommandLine`
- Comandos existentes registados em `Program.cs`
- Sem utilitários Docker actualmente

### Restrições
- Deve usar `System.Diagnostics.Process` para chamadas Docker (já presente)
- Imagem `postgres:alpine` para economia de espaço
- Locale pt-BR obrigatório para o container

## Goals / Non-Goals

**Goals:**
- Criar subcomando `devmaid docker postgres` que provisiona PostgreSQL em um comando
- Validar disponibilidade do Docker antes de executar
- Configurar container com locale pt-BR e logging completo
- Garantir persistência de dados via volume Docker

**Non-Goals:**
- Interface de gerenciamento de containers (start/stop/status)
- Suporte a múltiplas instâncias do PostgreSQL
- Configuração de credenciais customizáveis via flags

## Decisões

### 1. Estrutura de Comandos Aninhados
**Decisão:** Criar `DockerCommand` como grupo pai com `DockerPostgresCommand` como subcomando.

**Rationale:** Segue o padrão existente da CLI (ex: `GitCommand` → `GitCloneCommand`). Permite futura expansão com outros utilitários Docker (`docker ps`, `docker logs`, etc).

**Alternativas consideradas:**
- Comando plano `devmaid postgres-docker`: Rejeitado por não seguir convenção existente e dificultar expansão

### 2. Validação Pré-Execução
**Decisão:** Verificar `docker info` antes de executar `docker run`.

**Rationale:** Falhar cedo com mensagem clara é melhor que erro críptico do Docker no meio do processo.

### 3. Configuração do Container
**Decisão:** Parâmetros fixos com valores padrão de desenvolvimento.

```csharp
docker run \
  --name postgres-ptbr \
  --restart always \
  -e POSTGRES_PASSWORD=dev \
  -e LANG=pt_BR.UTF-8 \
  -e LC_ALL=pt_BR.UTF-8 \
  -p 5432:5432 \
  -v postgres-data:/var/lib/postgresql/data \
  postgres:alpine
```

**Rationale:** Valores fixos reduzem complexidade. Credenciais "dev" são apropriadas para ambiente local. Volume nomeado é mais simples que bind mount.

### 4. Tratamento de Container Já Existente
**Decisão:** Verificar se container `postgres-ptbr` já existe e, se existir e parado, iniciar ao invés de criar novo.

**Rationale:** UX consistente: usuário executa comando → obtiene PostgreSQL pronto. Evita erro "container name already in use".

## Risks / Trade-offs

**[Risco]** Docker não instalado → **Mitigação:** Mensagem clara "Docker não encontrado. Instale o Docker Desktop."

**[Risco]** Porta 5432 já em uso → **Mitigação:** Capturar erro e sugerir verificar processos usando a porta.

**[Risco]** Container preso em estado inválido → **Mitigação:** Oferecer flag `--force` para remover e recriar container.

**[Trade-off]** Parâmetros fixos vs. customizáveis: Simplicidade vs. flexibilidade. Priorizamos simplicidade para MVP.

## Migration Plan

1. Adicionar `DockerCommand` ao registro de comandos em `Program.cs`
2. Implementar `DockerPostgresCommand` com validação e execução
3. Testar com Docker Desktop parado (verifica mensagem de erro)
4. Testar com container já existente
5. Testar primeiro provisionamento completo

**Rollback:** Remover registo em `Program.cs` e excluir arquivos do comando.

## Open Questions

1. Devemos expor porta via flag (ex: `--port 5433`)? Decidido: não para MVP
2. Flag `--force` para forçar recriação? Pendente de validação com usuários
3. Variáveis de ambiente customizáveis (POSTGRES_USER, POSTGRES_DB)? Futuras versões
