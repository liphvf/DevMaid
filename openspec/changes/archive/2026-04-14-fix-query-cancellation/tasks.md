## 1. Corrigir propagação do CancellationToken

- [x] 1.1 Em `QueryCommand.cs`, adicionar `CancellationToken = cts.Token` ao `ParallelOptions` externo (linha ~414) do `Parallel.ForEachAsync` por servidor

## 2. Corrigir o ResiliencePipeline do Polly

- [x] 2.1 Em `QueryCommand.cs`, remover `OperationCanceledException` do `ShouldHandle` do `ResiliencePipeline` estático, mantendo apenas `NpgsqlException` e `TimeoutException`

## 3. Corrigir o await do writer task

- [x] 3.1 Em `QueryCommand.cs`, substituir `await writerCompleted.Task` por `await writerCompleted.Task.WaitAsync(cts.Token)` para evitar travamento após cancelamento

## 4. Verificação

- [x] 4.1 Executar `fur query run` com uma query longa e confirmar que Ctrl+C interrompe a execução imediatamente
- [x] 4.2 Confirmar que o processo encerra com exit code 130
- [x] 4.3 Confirmar que falhas transientes de rede ainda são retentadas (comportamento do Polly preservado)
- [x] 4.4 Executar os testes existentes e confirmar que não há regressões
