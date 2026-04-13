## Por que

O comando `query run` acumula todos os resultados em memória e só escreve arquivos CSV após todas as queries terminarem. Isso significa que: (1) se o processo crasha no meio, todos os resultados são perdidos; (2) o usuário fica sem feedback visual claro do progresso durante execuções longas; (3) erros não ficam registrados em arquivo algum. O usuário precisa ver resultados sendo salvos em tempo real e ter um arquivo de log persistente para diagnóstico.

## O que Muda

- **BREAKING**: Saída de arquivos passa a usar subpasta por execução (`results/<timestamp>/`) em vez de arquivo único direto em `results/`
- **BREAKING**: Parâmetro `--separate-files` é removido; CSV por servidor com append progressivo é agora o comportamento padrão
- Arquivos CSV por servidor são escritos com append progressivo após cada query completar (header inconsistente é aceitável)
- Arquivo CSV consolidado é gerado ao final (merge dos parciais)
- Arquivo de erros (`<timestamp>_erros.csv`) é escrito progressivamente (append a cada falha)
- Feedback visual passa de linhas AnsiConsole para progress bar Spectre com feed de atividades
- Log de execução em arquivo CSV progressivo (Server, Database, ExecutedAt, Status, RowCount, Duration, Error)

## Capacidades

### Novas Capacidades
- `query-run-streaming-csv`: Escrita progressiva de CSVs por servidor (append) e geração de CSV consolidado ao final
- `query-run-execution-log`: Log de execução em arquivo CSV escrito progressivamente, incluindo erros
- `query-run-progress-feedback`: Feedback visual com progress bar Spectre e feed de atividades em tempo real

### Capacidades Modificadas
- `query-run-csv-export`: Formato de saída muda de arquivo único para subpasta com CSVs por servidor (append progressivo) + consolidado; `--separate-files` é removido (comportamento absorvido como padrão); erros passam a ter arquivo próprio
- `query-run-multi-server-execution`: Adição de tracking de duração por query e persistência de erros em arquivo

## Impacto

- `FurLab.CLI/Commands/QueryCommand.cs`: Refatoração principal — remoção de `--separate-files`, nova lógica de escrita progressiva, Channel-based writer, progress bar
- `FurLab.CLI/Commands/CsvExporter.cs`: Novos métodos para append progressivo em CSV por servidor e merge de consolidado
- `FurLab.CLI/Commands/CsvRow.cs`: Adição de campo `Duration` e possível adaptação para escrita progressiva
- `FurLab.CLI/CommandOptions/QueryCommandOptions.cs`: Remoção de `SeparateFiles`
- `FurLab.Tests/Commands/QueryCommandTests.cs`: Atualização de testes para novo comportamento
- `FurLab.Tests/Commands/CsvExportTests.cs`: Novos testes para CSV parcial, consolidado e erros