# Spec: query-run-execution-log

## Purpose

Define o comportamento de logging progressivo de execução de queries: arquivo de erros dedicado escrito por append a cada falha, e log de execução em CSV escrito progressivamente para cada query completada (sucesso ou falha), ambos com AutoFlush para garantir durabilidade.

## Requirements

### Requirement: Arquivo de erros progressivo
O sistema DEVE escrever um arquivo CSV de erros progressivamente, appending uma linha a cada falha de query. Cada escrita DEVE fazer flush para o disco.

#### Cenário: Query falha em servidor/database
- **QUANDO** uma query falha em um servidor/database
- **ENTÃO** sistema adiciona linha ao arquivo `results/<timestamp>/<timestamp>_erros.csv`
- **E** linha contém colunas: Server, Database, ExecutedAt, Error
- **E** escrita é imediata (não acumula até o final)

#### Cenário: Múltiplas falhas
- **QUANDO** múltiplas queries falham
- **ENTÃO** cada falha gera uma linha no arquivo de erros
- **E** linhas são escritas na ordem em que as falhas ocorrem
- **E** arquivo é atualizado progressivamente pelo consumidor do Channel

#### Cenário: Nenhuma falha
- **QUANDO** todas as queries executam com sucesso
- **ENTÃO** arquivo `_erros.csv` NÃO é criado

### Requirement: Log de execução em arquivo CSV
O sistema DEVE manter um log de execução em arquivo CSV com entradas escritas progressivamente a cada query completada (sucesso ou falha). Cada escrita DEVE fazer flush para o disco.

#### Cenário: Query completa com sucesso
- **QUANDO** uma query executa com sucesso
- **ENTÃO** sistema adiciona linha ao log com: Server, Database, ExecutedAt, Status=Success, RowCount, Duration
- **E** coluna Error fica vazia

#### Cenário: Query falha
- **QUANDO** uma query falha
- **ENTÃO** sistema adiciona linha ao log com: Server, Database, ExecutedAt, Status=Error, RowCount=0, Duration, Error=<mensagem>

#### Cenário: Tracking de duração
- **QUANDO** uma query inicia execução
- **ENTÃO** sistema mede tempo entre início e fim da execução
- **E** duração é registrada no campo Duration do log
- **E** formato de Duration DEVE ser legível (e.g., "2.3s", "150ms")
