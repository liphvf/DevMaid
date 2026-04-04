# Pesquisa: Comando CLI para Configurar pgpass

**Feature**: `011-pgpass-cli-setup` | **Data**: 2026-04-04  
**Fase**: Phase 0 — Resolução de incógnitas técnicas

---

## 1. Formato do pgpass.conf

### Decisão
Entradas no formato `hostname:porta:banco:usuario:senha`, uma por linha. Caracteres `:` e `\` devem ser escapados como `\:` e `\\` respectivamente. Sem outros escapes necessários.

### Racional
Especificação oficial do PostgreSQL (libpq-pgpass). O PostgreSQL usa a **primeira linha que corresponder** aos parâmetros de conexão — entradas mais específicas devem vir antes de entradas com curinga.

### Detalhes técnicos
- **Campos 1–4** (`hostname`, `porta`, `banco`, `usuario`): aceitam `*` como curinga
- **Campo 5** (`senha`): nunca é curinga — sempre o valor literal
- **Comentários**: linhas iniciadas com `#` são ignoradas
- **Caminho Windows**: `%APPDATA%\postgresql\pgpass.conf` (ex.: `C:\Users\<usuario>\AppData\Roaming\postgresql\pgpass.conf`)
- **Permissões**: Windows não verifica permissões de arquivo (diferente de Unix, que exige `0600`)
- **Codificação**: UTF-8 / locale do sistema (libpq não impõe limite de linha)
- **Wildcard `localhost`**: conexões sem `host` especificado correspondem à string `localhost` no arquivo

### Alternativas Consideradas
- Usar `PGPASSWORD` em vez de pgpass: descartado pois pgpass é persistente e a spec é explícita
- Gerenciar permissões de arquivo como no Unix: não aplicável no Windows

---

## 2. Padrão de Comando — Subcomando Aninhado em `DatabaseCommand`

### Decisão
`PgPassCommand` será uma `static class` com método `Build() → Command` retornando o subcomando `pgpass` com três sub-subcomandos (`add`, `list`, `remove`). **O comando é aninhado dentro de `DatabaseCommand`**, resultando na hierarquia `devmaid database pgpass <ação>`. `DatabaseCommand.Build()` é modificado minimamente para incluir `PgPassCommand.Build()`.

### Racional
Decisão explícita do usuário: o pgpass é uma operação de banco de dados e logicamente pertence ao grupo `database`. O agrupamento por domínio (`database`) é mais intuitivo que um comando de topo de nível independente. A modificação em `DatabaseCommand.Build()` é de uma única linha.

### Estrutura
```csharp
// DatabaseCommand.cs — modificação mínima
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

// PgPassCommand.cs — novo arquivo
public static class PgPassCommand
{
    public static Command Build()   // retorna Command("pgpass", ...)
    // BuildAddCommand() → Command
    // BuildListCommand() → Command
    // BuildRemoveCommand() → Command
    public static void Add(PgPassAddOptions options)
    public static void List(PgPassListOptions options)
    public static void Remove(PgPassRemoveOptions options)
}
```

### Alternativas Consideradas
- `devmaid pgpass` como comando de topo de nível: descartado por decisão explícita do usuário — pgpass é operação de banco de dados
- Classe instanciável com DI: padrão não usado no projeto; seria inconsistente com os demais comandos

---

## 3. Padrão de Serviço — Interface + Implementação Concreta

### Decisão
`IPgPassService` em `DevMaid.Core/Interfaces/` com implementação `PgPassService` em `DevMaid.Core/Services/`. Registrado como singleton em `ServiceCollectionExtensions.AddDevMaidServices()`.

### Racional
A Constituição exige que todo serviço seja definido por interface em `Core/Interfaces/`. O padrão de registro via `AddDevMaidServices()` é o ponto centralizado de DI do projeto.

### Interface proposta
```csharp
public interface IPgPassService
{
    PgPassResult AddEntry(PgPassEntry entry);
    IReadOnlyList<PgPassEntry> ListEntries();
    PgPassResult RemoveEntry(string hostname, string port, string database, string username);
}
```

### Alternativas Consideradas
- Operações de arquivo diretas no Command: viola Constituição Artigo II (lógica no Command)
- Métodos assíncronos: desnecessário para operações locais de arquivo < 1s; YAGNI

---

## 4. Tratamento de Senha — Flag Opcional com Prompt Interativo

### Decisão
Senha implementada como `Option<string?>` opcional. Se não fornecida, solicitar interativamente via `PostgresPasswordHandler.ReadPasswordInteractively()` (existente). Senha nunca aparece em logs.

### Racional
A Constituição (Artigo I) proíbe opções de senha obrigatórias na CLI. O `PostgresPasswordHandler` já implementa leitura segura de senha com mascaramento de caracteres.

### Implementação
```
--password / -W  (opcional, string)
Se ausente: Console.Write("Senha: "); password = PostgresPasswordHandler.ReadPasswordInteractively();
```

### Alternativas Consideradas
- Senha como argumento posicional obrigatório: viola Constituição Artigo I
- `SecureString`: `PostgresPasswordHandler.ReadSecurePasswordInteractively()` disponível, mas o arquivo pgpass requer string plana; conversão via `SecureStringToString()` se necessário

---

## 5. Validação de Parâmetros

### Decisão
Reusar `SecurityUtils.IsValidHost()` e `SecurityUtils.IsValidPort()` já existentes. Adicionar validação de senha não-vazia no `PgPassService` antes de escrever.

### Racional
`SecurityUtils` já implementa proteção contra path traversal e validação de identificadores PostgreSQL. Consistência com o restante do projeto.

### Regras de validação
| Campo | Regra |
|-------|-------|
| `hostname` | `SecurityUtils.IsValidHost()` se fornecido; padrão `localhost` se omitido |
| `porta` | `SecurityUtils.IsValidPort()` se fornecida; padrão `5432` se omitida |
| `banco` | Não-vazio se fornecido; aceita `*` como curinga |
| `usuario` | Não-vazio se fornecido; padrão `postgres` se omitido |
| `senha` | Nunca vazia; solicitada interativamente se ausente na CLI |

---

## 6. Detecção de Duplicatas

### Decisão
Chave de unicidade: `(hostname, porta, banco, usuario)` — os 4 primeiros campos. Se uma entrada com a mesma chave existir, exibir mensagem informativa e retornar sem modificar o arquivo. Sair com código `0` (não é um erro; é comportamento esperado).

### Racional
RF-006 define que duplicatas devem ser informadas e ignoradas. Sair com `1` seria incorreto pois o estado desejado já existe.

### Alternativas Consideradas
- Substituir a senha da entrada existente: ambíguo; spec não menciona "atualizar" — apenas "adicionar" e "ignorar duplicata"

---

## 7. Estratégia de I/O de Arquivo

### Decisão
- **Leitura**: `File.ReadAllLines()` + parse manual linha a linha
- **Escrita (add)**: `File.AppendAllText()` — adicionar ao final, preservando entradas existentes
- **Escrita (remove)**: `File.WriteAllLines()` com lista filtrada (reescrita total)
- **Criar diretório**: `Directory.CreateDirectory()` — idempotente, não falha se já existe
- **Codificação**: `Encoding.UTF8` explícito em todas as operações

### Racional
Operações simples de arquivo sem dependências adicionais. A spec define explicitamente que novas entradas vão ao final (RF-005). Reescrita total para remoção é segura dado o tamanho esperado do arquivo.

### Alternativas Consideradas
- Usar `StreamWriter` com `lock` para concorrência: YAGNI — CLI é single-process, sem concorrência real
- Escrever arquivo temporário + rename atômico: overhead desnecessário para arquivo pequeno local

---

## 8. Mascaramento de Senha na Listagem

### Decisão
Exibir `****` no lugar da senha para todos os subcomandos que mostram entradas ao usuário.

### Racional
RF-007 e Constituição Artigo VI (segurança) exigem que senhas nunca apareçam em saída ou logs.

---

## 9. Estrutura de Testes

### Decisão
- `DevMaid.Tests/Commands/PgPassCommandTests.cs` — testes unitários do Command (parsing de opções, nomes de subcomandos, fluxo de chamada ao serviço com Moq)
- `DevMaid.Tests/Commands/PgPassServiceTests.cs` — testes unitários + integração do Service (usando diretório temporário `Path.GetTempPath()`)

### Racional
Padrão idêntico ao `DatabaseCommandTests.cs`. Testes de integração de arquivo usam diretório temporário real (Constituição Artigo III proíbe substituir testes de integração por mocks para operações de arquivo).

### Nomenclatura
```
AddEntry_ComEntradaValida_DeveCriarArquivoComFormatoCorreto
AddEntry_ComEntradaDuplicada_DeveRetornarMensagemInformativa
AddEntry_ComSenhaVazia_DeveRetornarErroDeSenhaVazia
AddEntry_ComCaracteresEspeciaisNaSenha_DeveEscaparCorretamente
RemoveEntry_ComEntradaExistente_DeveRemoverSomenteEntradaAlvo
ListEntries_ComArquivoVazio_DeveRetornarListaVazia
```

---

## Incógnitas Resolvidas

| Incógnita | Resolução |
|-----------|-----------|
| Formato exato de escape | `:` → `\:`, `\` → `\\` (especificação PostgreSQL) |
| Senha obrigatória vs. Constituição | Senha como flag opcional com prompt interativo |
| Chave de duplicata | `(hostname, porta, banco, usuario)` — 4 campos |
| Código de saída para duplicata | `0` — estado desejado já existe |
| Permissões de arquivo no Windows | Sem verificação necessária (diferente de Unix) |
| Estratégia de I/O | Append para add, reescrita total para remove |
| Posição na hierarquia CLI | Aninhado em `database` → `devmaid database pgpass <ação>` (decisão do usuário) |
