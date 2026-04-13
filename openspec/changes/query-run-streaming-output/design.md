## Contexto

O comando `query run` execute queries SQL em múltiplos servidores PostgreSQL e databases em paralelo. Atualmente, todos os resultados são acumulados em memória (`List<CsvRow>`) e escritos em um único CSV consolidado **após** todas as queries terminarem. Não existe arquivo de log persistente — todo feedback vai para o console via Spectre.Console. O parâmetro `--separate-files` permite gerar um CSV por servidor, mas também é batch-no-final.

O fluxo atual é:
1. Acumula `allResults` em memória com `lock(lockObj)`
2. Após `Parallel.ForEachAsync` completar, filtra success → escreve CSV
3. Erros: excluídos do CSV, aparecem só na tabela resumo no terminal

Arquivos afetados: `QueryCommand.cs` (738 linhas), `CsvExporter.cs` (128 linhas), `CsvRow.cs` (14 linhas), `QueryCommandOptions.cs` (75 linhas).

## Goals / Non-Goals

**Goals:**
- Escrever CSV parcial por servidor com append progressivo imediatamente após cada query completar
- Gerar CSV consolidado ao final (merge dos parciais)
- Gerar arquivo de erros `_erros.csv` progressivamente (append a cada falha)
- Prover feedback visual com progress bar Spectre + feed de atividades
- Organizar saída em subpasta por execução (`results/<timestamp>/`)
- Adicionar tracking de duração por query

**Non-Goals:**
- Streaming de rows durante a execução da query (linha por linha do result set)
- Mudar o modelo de conexão ou retry (Polly)
- Alterar interação de seleção de servidores
- Suportar outros formatos de exportação (JSON, Parquet, etc.)
- Mudar o comportamento do log estruturado do `Microsoft.Extensions.Logging`

## Decisions

### D1: Channel<CsvRow> como padrão produtor-consumidor

Usar `System.Threading.Channels.Channel<CsvRow>` bounded para comunicação entre tasks paralelas (produtoras) e uma task dedicada escritora (consumidora).

**Por que não `lock + List + write-imediato`?** I/O dentro do `Parallel.ForEachAsync` com lock serializa todas as threads de execução no I/O. Se o disco for lento (rede, USB), threads ficam bloqueadas esperando I/O ao invés de executar a próxima query. O Channel desacopla: produtores só fazem `channel.Writer.TryWrite()`, que é O(1), e o consumidor dedicado escreve sequencialmente sem lock.

**Por que não `ActionBlock<T>` (TPL Dataflow)?** TPL Dataflow adiciona uma dependência extra. `Channel<T>` é nativo do .NET desde Core 3.0, sem pacotes adicionais.

**Bounded vs Unbounded:** Bounded com capacidade = `maxDegreeOfParallelism * 2`. Aplica backpressure natural — se o escritor não acompanha, produtores esperam ao invés de consumir memória infinita.

### D2: Subpasta por execução

Cada execução cria `results/<timestamp>/` com todos os arquivos daquela execução.

**Por que?** Isola execuções, evita conflitos de nomes se o usuário rodar o comando duas vezes em sequência rápida, e facilita auditoria (uma pasta = uma execução).

**Formato:** `results/2026-04-13_143022/` — mesmo formato de timestamp atual.

### D3: CSV parcial por servidor com append progressivo

Cada servidor tem um arquivo CSV parcial `results/<ts>/<server>_<ts>.csv`. A cada query que completa com sucesso naquele servidor, o resultado é feito append no arquivo (sem reescrever header). Se o arquivo não existe, header é escrito antes dos dados.

**Header inconsistente é aceitável:** Diferentes databases no mesmo servidor podem retornar colunas diferentes. Cada append escreve os dados conforme as colunas da query daquele database. Se um database traz colunas extras, o header fica inconsistente com relação aos dados anteriores. Isso é aceitável — o consolidado ao final resolve isso com header unificado.

**Por que por servidor e não por database?** Menos arquivos (1 por servidor em vez de 1 por database). Para 3 servidores com 50 databases, são 3 arquivos em vez de 50. E por servidor faz sentido organizacionalmente — agrupa resultados do mesmo servidor.

**Append ao invés de recriar:** Usa `StreamWriter` com `append=true`. Header só é escrito na primeira vez (verifica se arquivo existe). Dados subsequentes são apenas append. Sem lock — single-writer via Channel.

**Flush imediato após cada escrita:** O `StreamWriter` DEVE ter `AutoFlush = true` ou chamar `Flush()` explicitamente após cada append. Isso garante que cada linha escrita vá para o disco imediatamente, sem ficar no buffer da aplicação. Sem flush, um crash do processo perde dados que foram "escritos" mas ainda estavam no buffer — o que contradiz a motivação principal de resiliência (dados salvos mesmo se o processo crashar).

**Nomenclatura:** `<server>_<timestamp>.csv`. Sanitização de nomes: substituir caracteres inválidos de filename por `_`.

### D4: CSV consolidado gerado por merge ao final

Após todas as queries terminarem, ler todos os CSVs parciais e escrever `consolidated_<timestamp>.csv`.

**Por que não streaming direto no consolidado?** O header do consolidado precisa conter a união de todas as colunas de todos os resultados. Os parciais por servidor podem ter headers inconsistentes (databases com colunas diferentes), então o consolidado precisa resolver isso com header unificado.

**E se o processo crashar antes do merge?** Os parciais por servidor já estão salvos. O usuário pode abrir os parciais individualmente. Isso é aceitável (confirmado pelo usuário).

**Implementação:** Ler cada CSV parcial, construir `BuildColumnList()` (união de colunas), escrever header + todos os dados. Reaproveitar lógica existente do `CsvExporter`. Para parciais com header inconsistente, usar o header do primeiro database e assumir que dados subsequentes podem ter mais ou menos colunas.

### D5: Arquivo de erros progressivo

Arquivo `results/<ts>/<timestamp>_erros.csv` com colunas: `Server, Database, ExecutedAt, Error`.

**Escrito pelo consumidor do Channel** junto com os CSVs parciais de sucesso. Cada CsvRow com `Status == "Error"` gera um append neste arquivo.

**Por que não incluir erros no CSV consolidado?** Misturar erros com dados de query quebra a estrutura colunar (erros não têm as colunas da query). Arquivo dedicado mantém cada CSV com schema consistente.

### D6: Progress bar Spectre com feed de atividades

Usar `Spectre.Console.Progress` com uma barra principal e uma task de exibição de atividades recentes.

```
Executing query across 3 servers, 47 databases...

████████████████████████░░░░░░  31/47  (65%)

✓ prod-pg-01/users     1,500 rows  (2.3s)
✓ prod-pg-01/orders    8,230 rows  (5.1s)
✗ prod-pg-02/logs      connection refused
⟳ prod-pg-03/sales    running... (3.2s)
```

**Implementação:** `AnsiConsole.Progress()` com `ProgressColumn` customizado showing completed/total. Feed de atividades como linhas renderizadas abaixo da barra. O Channel consumer atualiza o progress.

**Por que não Spectre Status spinner?** Spinner mostra "working..." mas não dá noção de progresso. Barra mostra quanto falta.

**Por que não tabela live?** Tabelas live do Spectre têm complexidade de renderização e flickering. Barra + feed é mais estável e mais informativo.

### D7: Remoção de `--separate-files`

O comportamento de `--separate-files` (um CSV por servidor) é agora o comportamento padrão — sempre gera CSV por servidor com append progressivo + consolidado final. A flag se torna redundante.

**Migração:** Usuários que usavam `--separate-files` terão saída idêntica (por servidor), mas agora progressiva e em subpasta.

### D8: Adição de Duration ao CsvRow

O `CsvRow` record ganha campo `Duration` (`TimeSpan` ou `double` em ms). Isso permite:
- Exibir duração no feed de atividades
- Registrar duração no log de execução
- Diagnosticar queries lentas

Medido como tempo entre `ExecuteQueryWithRetryAsync` start e completion.

## Riscos / Trade-offs

- **[Risco: Crash antes do consolidado] → Mitigação:** CSVs parciais já estão salvos; usuário pode abri-los individualmente ou rodar um merge manual.
- **[Risco: Número grande de arquivos parciais] → Mitigação:** Um arquivo por servidor, não por database. Para 3 servidores são 3 arquivos, independente do número de databases.
- **[Risco: Consumo de I/O por escritas parciais] → Mitigação:** Cada parcial é pequeno (resultado de uma query). Channel bounded limita fila. Disco local raramente é gargalo.
- **[Risco: Concurrent writes acidentais no mesmo arquivo] → Mitigação:** Single-writer via Channel consumer elimina concorrência no I/O.
- **[Trade-off: Leitura de CSVs para merge ao final] → Consome I/O extra, mas é uma leitura sequencial de arquivos pequenos, insignificante comparado ao tempo de query.
- **[Trade-off: API breaking change `--separate-files`] → Impacto baixo: flag é removida, mas comportamento é suprido (e melhorado) pelo novo padrão.