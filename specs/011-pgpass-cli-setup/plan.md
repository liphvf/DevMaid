# Plano de Implementação: Comando CLI para Configurar pgpass

**Branch**: `011-pgpass-cli-setup` | **Data**: 2026-04-04 | **Spec**: [spec.md](./spec.md)  
**Entrada**: Especificação de feature em `specs/011-pgpass-cli-setup/spec.md`

---

## Sumário

Implementar o subcomando `devmaid database pgpass` com três sub-subcomandos (`add`, `list`, `remove`) que gerenciam o arquivo `%APPDATA%\postgresql\pgpass.conf` no Windows. O comando `pgpass` é aninhado dentro do `DatabaseCommand` existente — `DatabaseCommand.Build()` receberá `PgPassCommand.Build()` como subcomando adicional. Apenas banco de dados e senha são obrigatórios; hostname (`localhost`), porta (`5432`) e usuário (`postgres`) têm padrões sensatos. A lógica de negócio (leitura/escrita/escape do arquivo) fica em `DevMaid.Core`; o comando CLI é um wrapper fino em `DevMaid.CLI`.

---

## Contexto Técnico

**Linguagem/Versão**: C# / .NET 10.0  
**Dependências Primárias**: `System.CommandLine` 2.0.5, `Microsoft.Extensions.Hosting` 10.0.5, `Microsoft.Extensions.DependencyInjection` 10.0.5, `Microsoft.Extensions.Logging` 10.0.5  
**Armazenamento**: Sistema de arquivos — `%APPDATA%\postgresql\pgpass.conf` (texto simples, uma entrada por linha)  
**Testes**: MSTest 4.1.0 + Moq 4.20.72  
**Plataforma Alvo**: Windows (exclusivo; pgpass em Linux/macOS está fora do escopo)  
**Tipo de Projeto**: CLI tool — nenhum projeto novo é introduzido; o código se integra a `DevMaid.CLI` e `DevMaid.Core` existentes  
**Metas de Desempenho**: Operações de arquivo local; sem requisitos de latência além de < 1s para qualquer subcomando  
**Restrições**: Sem dependências externas novas (nenhum NuGet adicional necessário); operações síncronas de arquivo são suficientes (arquivo pequeno, local)  
**Escala/Escopo**: Arquivo com dezenas a centenas de entradas no máximo; zero preocupações de escala

---

## Verificação da Constituição

*GATE: Deve passar antes da Phase 0. Re-verificar após Phase 1.*

| Princípio | Status | Observação |
|-----------|--------|------------|
| **I. CLI em Primeiro Lugar** — padrão `devmaid <substantivo> <verbo>` | PASSA | Comandos: `devmaid database pgpass add`, `devmaid database pgpass list`, `devmaid database pgpass remove` — aninhado dentro de `database` por decisão do usuário |
| **I. Opções de senha** — nunca obrigatórias na CLI; solicitar interativamente se ausentes | ATENÇÃO | Spec define senha como obrigatória via argumento. Resolução: senha deve ser flag com solicitação interativa de fallback, conforme constituição — ver RF-001 e Artigo I |
| **I. Códigos de saída** — `0` sucesso, `1` erro geral, `2` args inválidos, `3` dep. ausente | PASSA | RF-009, RF-011, RF-012, RF-013 cobrem todos os caminhos de erro |
| **II. Arquitetura Modular** — Command wrapper fino, Service com lógica | PASSA | `PgPassCommand.cs` retorna subcomando inserido em `DatabaseCommand.Build()`; `IPgPassService` / `PgPassService` em Core |
| **II. Interface obrigatória** — todo Service definido por interface em `Core/Interfaces/` | PASSA | `IPgPassService` em `DevMaid.Core/Interfaces/` |
| **II. Sem novos projetos** — máximo 1 por feature | PASSA | Zero novos projetos; integração em projetos existentes |
| **III. Test-First** — testes unitários antes/junto com a implementação | PASSA | `PgPassCommandTests.cs` em `DevMaid.Tests/Commands/`; testes de integração para I/O de arquivo |
| **IV. Erros Acionáveis** — indicar o quê falhou + o que fazer | PASSA | RF-011 (senha vazia), RF-012 (permissão → "execute como administrador"), RF-013 (arquivo travado) |
| **V. YAGNI** — sem abstrações hipotéticas | PASSA | Sem padrão Repository; acesso direto a arquivo via `File.*` no Service |
| **VI. ILogger** — logging via `ILogger` do Core | PASSA | `PgPassService` recebe `ILogger` via injeção de construtor primário |
| **VI. CancellationToken** — ops assíncronas respeitam cancelamento | APLICÁVEL | Operações de arquivo são síncronas por natureza; async wrappers desnecessários para < 1s |
| **VI. Segurança** — senhas nunca em logs | PASSA | Senha nunca deve aparecer em mensagens de log ou saída CLI |
| **VII. Configuração** — senhas não em appsettings | PASSA | Credenciais passadas apenas via CLI flag / prompt interativo; não persistidas em config |

**Resolução da ATENÇÃO — Senha Obrigatória vs. Constituição:**  
A Constituição (Artigo I) proíbe opções de senha obrigatórias — elas devem ser solicitadas interativamente quando não fornecidas. A senha será implementada como uma `Option<string>` opcional com fallback de prompt interativo (padrão do projeto, via `PostgresPasswordHandler` existente). O cenário de aceitação `localhost:5432:banco:postgres:senha` continua válido — o usuário pode fornecer a senha via flag, mas não é bloqueado se omitir.

---

## Estrutura do Projeto

### Documentação (esta feature)

```text
specs/011-pgpass-cli-setup/
├── plan.md              # Este arquivo
├── research.md          # Saída Phase 0
├── data-model.md        # Saída Phase 1
├── quickstart.md        # Saída Phase 1
├── contracts/           # Saída Phase 1
│   └── pgpass-command.md
└── tasks.md             # Saída Phase 2 (/tasks — NÃO criado por /plan)
```

### Código-Fonte

```text
DevMaid.CLI/
├── Commands/
│   ├── DatabaseCommand.cs            # MODIFICADO — adiciona PgPassCommand.Build() como subcomando
│   └── PgPassCommand.cs              # NOVO — wrapper CLI para add/list/remove
└── CommandOptions/
    └── PgPassCommandOptions.cs       # NOVO — DTOs fortemente tipados para cada subcomando

DevMaid.Core/
├── Interfaces/
│   └── IPgPassService.cs             # NOVO — contrato do serviço
├── Models/
│   └── PgPassEntry.cs                # NOVO — entidade de domínio
└── Services/
    └── PgPassService.cs              # NOVO — lógica de negócio (leitura/escrita/escape/validação)

DevMaid.Tests/
└── Commands/
    ├── PgPassCommandTests.cs         # NOVO — testes unitários do comando CLI
    └── PgPassServiceTests.cs         # NOVO — testes unitários do serviço (+ integração de arquivo)
```

**Decisão de Estrutura**: Opção 1 (Single Project). Nenhum novo projeto adicionado. `PgPassCommand` é subcomando de `DatabaseCommand`, seguindo a hierarquia `devmaid database pgpass <ação>`. `DatabaseCommand.Build()` é modificado para incluir `PgPassCommand.Build()`:

```csharp
// DatabaseCommand.cs — alteração mínima
public static Command Build()
{
    var command = new Command("database", "Database utilities.")
    {
        BuildBackupCommand(),
        BuildRestoreCommand(),
        PgPassCommand.Build()   // ← adicionado
    };
    return command;
}
```

---

## Rastreamento de Complexidade

> Nenhuma violação justificada necessária. Todos os gates passam.
