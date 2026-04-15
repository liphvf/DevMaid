## Contexto

O `QueryCommand` cria um `CancellationTokenSource` interno e registra um handler de `Console.CancelKeyPress` para cancelar o token quando o usuário pressiona Ctrl+C. Porém, há três pontos onde o token não chega corretamente:

1. O `ParallelOptions` do loop externo (`Parallel.ForEachAsync` por servidor, linha 414) não recebe `CancellationToken`, fazendo com que o `ct` injetado pelo framework nos lambdas seja `CancellationToken.None`.
2. O `ResiliencePipeline` do Polly tem `ShouldHandle` que inclui `OperationCanceledException`, causando até 3 retentativas (com backoff de 500ms, 1000ms, 2000ms) após o cancelamento.
3. O `await writerCompleted.Task` não recebe nenhum token, podendo travar indefinidamente após as queries serem interrompidas.

## Objetivos / Não-Objetivos

**Objetivos:**
- Ctrl+C deve interromper as queries em andamento imediatamente (no cliente e no servidor PostgreSQL)
- O processo deve encerrar limpo, sem travar, após o cancelamento
- O Polly não deve retentar operações canceladas intencionalmente

**Não-Objetivos:**
- Não alterar a lógica de retry para falhas transientes de rede (`NpgsqlException`, `TimeoutException`)
- Não adicionar confirmação ou prompt antes de cancelar
- Não alterar comportamento de cancelamento em outros comandos (`DatabaseCommand`, etc.)

## Decisões

### Decisão 1: Passar `cts.Token` ao `ParallelOptions` externo

O `ParallelOptions` da linha 414 deve receber `CancellationToken = cts.Token`. Isso faz com que o `ct` injetado pelo framework nos lambdas seja o token real, que é repassado corretamente ao loop interno e às chamadas Npgsql.

**Alternativa considerada:** Capturar `cts.Token` por closure no lambda externo em vez de usar `ParallelOptions.CancellationToken`. Rejeitado porque o `Parallel.ForEachAsync` não interromperia o agendamento de novas iterações — seria necessário checar manualmente o token no início de cada lambda.

---

### Decisão 2: Remover `OperationCanceledException` do `ShouldHandle` do Polly

O `ResiliencePipeline` estático deve tratar apenas `NpgsqlException` e `TimeoutException` como erros retryáveis. `OperationCanceledException` representa cancelamento intencional e não deve ser retentado.

**Alternativa considerada:** Criar um segundo pipeline sem retry para uso com cancelamento. Rejeitado por complexidade desnecessária — o caminho certo é não retentar cancelamentos nunca.

---

### Decisão 3: Passar o token ao `writerCompleted.Task` via `WaitAsync`

Substituir `await writerCompleted.Task` por `await writerCompleted.Task.WaitAsync(cts.Token)` para garantir que o processo não trave após cancelamento. Se o writer não drenar o channel a tempo, o `WaitAsync` lança `OperationCanceledException` e o processo encerra.

**Alternativa considerada:** Adicionar um timeout fixo (ex: 5s). Rejeitado porque o comportamento correto é respeitar o mesmo token de cancelamento já em uso, sem introduzir valores mágicos.

## Riscos / Trade-offs

- **[Risco] Perda de linhas já lidas mas não escritas no CSV** → Após cancelamento, o channel pode conter linhas em trânsito que não serão persistidas. Mitigação: aceitável — o usuário pediu para parar; o log e o CSV ficam parciais, mas a execução encerra limpa.
- **[Trade-off] `WaitAsync` pode lançar se o writer ainda não drenou** → O writer task continuará rodando em background por um momento até o GC coletar. Não causa vazamento — o processo encerra logo após o `OperationCanceledException` ser capturado pelo `Program.Main`.
