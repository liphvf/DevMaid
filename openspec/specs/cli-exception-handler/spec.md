## Propósito

Tratamento centralizado de exceções com mapeamento para códigos de saída específicos via SetExceptionHandler, fornecendo tratamento de erros consistente em todos os comandos CLI.

## Requisitos

### Requisito: SetExceptionHandler centraliza mapeamento de exceções a códigos de saída
O sistema DEVE configurar `config.SetExceptionHandler((ex, resolver) => ...)` no `CommandApp` para interceptar todas as exceções não tratadas durante parsing e execução, retornando códigos de saída específicos por tipo de exceção.

#### Cenário: Exceção de banco de dados retorna código de saída específico
- **QUANDO** uma `NpgsqlException` com `SqlState` não nulo é lançada durante execução de um comando
- **ENTÃO** o processo DEVE terminar com código de saída `10`

#### Cenário: Exceção de banco de dados genérica retorna código de saída específico
- **QUANDO** uma `NpgsqlException` sem `SqlState` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `11`

#### Cenário: Exceção de I/O retorna código de saída específico
- **QUANDO** uma `IOException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `20`

#### Cenário: Exceção de diretório não encontrado retorna código de saída específico
- **QUANDO** uma `DirectoryNotFoundException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `21`

#### Cenário: Exceção de arquivo não encontrado retorna código de saída específico
- **QUANDO** uma `FileNotFoundException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `22`

#### Cenário: Exceção de permissão retorna código de saída específico
- **QUANDO** uma `UnauthorizedAccessException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `30`

#### Cenário: Operação inválida retorna código de saída específico
- **QUANDO** uma `InvalidOperationException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `40`

#### Cenário: Argumento inválido retorna código de saída específico
- **QUANDO** uma `ArgumentException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `41`

#### Cenário: Timeout retorna código de saída específico
- **QUANDO** uma `TimeoutException` é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `50`

#### Cenário: Cancelamento pelo usuário retorna código de saída específico
- **QUANDO** uma `OperationCanceledException` é lançada (ex: Ctrl+C)
- **ENTÃO** o processo DEVE terminar com código de saída `130`

#### Cenário: Exceção genérica retorna código de saída padrão
- **QUANDO** qualquer outra exceção não mapeada é lançada
- **ENTÃO** o processo DEVE terminar com código de saída `1`

---

### Requisito: Erros são exibidos com formatação Spectre.Console
O sistema DEVE utilizar `AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths)` dentro do `SetExceptionHandler` para exibir exceções de forma visualmente clara e consistente.

#### Cenário: Exceção exibe stack trace formatado
- **QUANDO** uma exceção não tratada é capturada pelo handler
- **ENTÃO** a mensagem de erro DEVE ser exibida no console com formatação visual (cores, paths encurtados) antes do processo terminar

#### Cenário: Log de erro quando resolver disponível
- **QUANDO** a exceção ocorre durante execução de comando (não durante parsing) e o `ITypeResolver` está disponível
- **ENTÃO** o handler DEVE tentar resolver `ILogger` via `resolver` e registrar o erro antes de retornar o código de saída
