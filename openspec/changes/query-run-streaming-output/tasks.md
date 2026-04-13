## 1. Estrutura e Modelos

- [ ] 1.1 Adicionar campo `Duration` (double, em ms) ao record `CsvRow`
- [ ] 1.2 Adicionar campo `LogFilePath` (string?) ao `CsvRow` ou criar modelo separado para o log de execução
- [ ] 1.3 Criar modelo `ExecutionLogEntry` com campos: Server, Database, ExecutedAt, Status, RowCount, Duration, Error (usado para `_erros.csv` e log)
- [ ] 1.4 Remover campo `SeparateFiles` de `QueryCommandOptions` e remoção da opção `--separate-files` no `QueryCommand.Build()`

## 2. Channel-based Writer

- [ ] 2.1 Criar `Channel<CsvRow>` bounded com capacidade = `maxDegreeOfParallelism * 2` em `ExecuteOnSelectedServers`
- [ ] 2.2 Implementar task consumidora dedicada que lê do Channel e escreve arquivos:
  - Se `Status == "Success"`: append dados no CSV parcial do servidor `results/<ts>/<server>_<ts>.csv` (cria header se arquivo não existe)
  - Se `Status == "Error"`: append linha em `results/<ts>/<ts>_erros.csv`
  - Para todo resultado: append linha no log de execução CSV
- [ ] 2.3 Implementar sanitização de nomes de servidor para filenames (substituir caracteres inválidos por `_`)
- [ ] 2.4 Garantir que `channel.Writer.Complete()` é chamado após todas as queries terminarem, e que a consumidora faz `WaitToReadAsync` até o channel completar

## 3. Escrita Progressiva de CSVs por Servidor

- [ ] 3.1 Criar método `CsvExporter.AppendToServerCsv(outputPath, CsvRow)` que faz append de dados no CSV do servidor (cria header + dados se arquivo não existe, apenas append dados se já existe)
- [ ] 3.2 Na task consumidora, chamar `AppendToServerCsv` para cada CsvRow de sucesso
- [ ] 3.3 Criar método `CsvExporter.WriteErrorEntry(outputPath, Server, Database, ExecutedAt, Error)` que faz append de uma linha no arquivo de erros (cria header se arquivo não existe)
- [ ] 3.4 Criar método `CsvExporter.WriteLogEntry(outputPath, ExecutionLogEntry)` que faz append de uma linha no log de execução (cria header se arquivo não existe)

## 4. Geração do CSV Consolidado

- [ ] 4.1 Após Channel completar (todas as queries processadas), ler todos os CSVs parciais por servidor da subpasta
- [ ] 4.2 Fazer merge dos parciais: construir lista de colunas (união), escrever `consolidated_<ts>.csv` com header unificado
- [ ] 4.3 Reaproveitar `CsvExporter.BuildColumnList` para união de colunas; adaptar para ler de arquivos parciais ou de `allResults` acumulado
- [ ] 4.4 Lidar com headers inconsistentes dos CSVs parciais: ao ler parciais com colunas diferentes, preencher campos faltantes com vazio

## 5. Organização em Subpasta

- [ ] 5.1 Modificar `ExecuteOnSelectedServers` para criar subpasta `results/<timestamp>/` antes de iniciar as queries
- [ ] 5.2 Atualizar todos os paths de output para usar subpasta: parciais, consolidado, erros, log
- [ ] 5.3 Garantir que `-o` especifica o diretório base, e a subpasta `<timestamp>/` é criada dentro dele

## 6. Progress Bar e Feed de Atividades

- [ ] 6.1 Calcular total de databases antes de iniciar (pré-contagem via `GetDatabasesForServerAsync`)
- [ ] 6.2 Implementar progress bar com `AnsiConsole.Progress()` — uma ProgressTask com incremento a cada query completada
- [ ] 6.3 Implementar feed de atividades abaixo da barra: últimas 4-5 atividades (sucesso/erro em execução)
- [ ] 6.4 Atualizar progress e feed a partir da task consumidora do Channel
- [ ] 6.5 Após conclusão, exibir resumo final: caminhos dos arquivos consolidado e erros, contagem de servidores/success/failed/rows

## 7. Tracking de Duração

- [ ] 7.1 Medir tempo de execução em `ExecuteQueryWithRetryAsync` (stopwatch antes/depois)
- [ ] 7.2 Incluir `Duration` no `CsvRow` enviado ao Channel
- [ ] 7.3 Exibir duração no feed de atividades (e.g., "1,500 rows (2.3s)")

## 8. Remoção de --separate-files

- [ ] 8.1 Remover definição da opção `--separate-files` no `QueryCommand.Build()`
- [ ] 8.2 Remover campo `SeparateFiles` de `QueryCommandOptions`
- [ ] 8.3 Remover lógica `if (options.SeparateFiles)` em `ExecuteOnSelectedServers`
- [ ] 8.4 Remover `WriteServerCsv` do `CsvExporter` se não for mais usado (verificar antes se consolidado usa)

## 9. Testes

- [ ] 9.1 Teste unitário: append progressivo em CSV por servidor (primeira escrita com header, subsequentes sem header)
- [ ] 9.2 Teste unitário: append de erro no arquivo `_erros.csv`
- [ ] 9.3 Teste unitário: append de entrada no log de execução
- [ ] 9.4 Teste unitário: merge de CSVs por servidor em consolidado com header unificado (colunas diferentes entre servidores)
- [ ] 9.5 Teste unitário: sanitização de nomes de servidor
- [ ] 9.6 Teste unitário: `Duration` medido corretamente
- [ ] 9.7 Atualizar testes existentes de `QueryCommandTests` para refletir novo comportamento (sem `--separate-files`, subpasta, etc.)
- [ ] 9.8 Atualizar testes de `CsvExportTests` para novos métodos