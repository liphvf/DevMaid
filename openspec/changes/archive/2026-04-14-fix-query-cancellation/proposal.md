## Por que

O comando `fur query run` não responde ao Ctrl+C durante a execução de queries. O `CancellationToken` criado pelo handler de Ctrl+C nunca é passado para o `Parallel.ForEachAsync` externo (por servidor), fazendo com que todas as queries em andamento continuem executando até o fim no PostgreSQL, independentemente de quantas vezes o usuário pressione Ctrl+C.

## O que Muda

- O `CancellationToken` do `CancellationTokenSource` interno deve ser passado ao `ParallelOptions` do loop externo (`Parallel.ForEachAsync` por servidor)
- O Polly não deve retentar `OperationCanceledException` — cancelamento é intencional, não uma falha transiente
- O `await writerCompleted.Task` deve respeitar o cancelamento para evitar travamento após as queries serem interrompidas

## Capacidades

### Novas Capacidades

Nenhuma.

### Capacidades Modificadas

- `query-run-multi-server-execution`: O comportamento de cancelamento via Ctrl+C muda — queries em andamento devem ser interrompidas imediatamente ao sinal de cancelamento, e o Polly não deve retentar operações canceladas intencionalmente.

## Impacto

- `FurLab.CLI/Commands/QueryCommand.cs`: ajustes no `ResiliencePipeline` (ShouldHandle), no `ParallelOptions` externo e no `await writerCompleted.Task`
