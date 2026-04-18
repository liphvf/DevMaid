## ADICIONADO Requisitos

### Requisito: SetExceptionHandler centraliza mapeamento de exceções a exit codes
O sistema DEVE configurar `config.SetExceptionHandler((ex, resolver) => ...)` no `CommandApp` para interceptar todas as exceções não tratadas tanto durante parsing quanto durante execução, retornando exit codes específicos por tipo de exceção.

#### Cenário: Exceção de banco de dados retorna exit code específico
- **QUANDO** uma `NpgsqlException` com `SqlState` não nulo é lançada durante execução de um command
- **ENTÃO** o processo DEVE terminar com exit code `10`

#### Cenário: Exceção de banco de dados genérica retorna exit code específico
- **QUANDO** uma `NpgsqlException` sem `SqlState` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `11`

#### Cenário: Exceção de I/O retorna exit code específico
- **QUANDO** uma `IOException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `20`

#### Cenário: Exceção de diretório não encontrado retorna exit code específico
- **QUANDO** uma `DirectoryNotFoundException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `21`

#### Cenário: Exceção de arquivo não encontrado retorna exit code específico
- **QUANDO** uma `FileNotFoundException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `22`

#### Cenário: Exceção de permissão retorna exit code específico
- **QUANDO** uma `UnauthorizedAccessException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `30`

#### Cenário: Operação inválida retorna exit code específico
- **QUANDO** uma `InvalidOperationException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `40`

#### Cenário: Argumento inválido retorna exit code específico
- **QUANDO** uma `ArgumentException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `41`

#### Cenário: Timeout retorna exit code específico
- **QUANDO** uma `TimeoutException` é lançada
- **ENTÃO** o processo DEVE terminar com exit code `50`

#### Cenário: Cancelamento pelo usuário retorna exit code específico
- **QUANDO** uma `OperationCanceledException` é lançada (ex: Ctrl+C)
- **ENTÃO** o processo DEVE terminar com exit code `130`

#### Cenário: Exceção genérica retorna exit code padrão
- **QUANDO** qualquer outra exceção não mapeada é lançada
- **ENTÃO** o processo DEVE terminar com exit code `1`

---

### Requisito: Erros são exibidos com formatação Spectre.Console
O sistema DEVE utilizar `AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths)` dentro do `SetExceptionHandler` para exibir exceções de forma visualmente clara e consistente.

#### Cenário: Exceção exibe stack trace formatado
- **QUANDO** uma exceção não tratada é capturada pelo handler
- **ENTÃO** a mensagem de erro DEVE ser exibida no console com formatação visual (cores, paths encurtados) antes do processo terminar

#### Cenário: Logging de erro quando resolver disponível
- **QUANDO** a exceção ocorre durante execução de command (não durante parsing) e o `ITypeResolver` está disponível
- **ENTÃO** o handler DEVE tentar resolver `ILogger` via `resolver` e registrar o erro antes de retornar o exit code
