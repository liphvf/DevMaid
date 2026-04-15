## MODIFICADO Requirements

### Requirement: Retry com Polly
O sistema DEVE usar Polly para retries automáticos em falhas transitórias de conexão. O sistema NÃO DEVE retentar operações canceladas intencionalmente pelo usuário.

#### Cenário: Falha transiente de rede
- **QUANDO** conexão falha por timeout ou erro transitório de rede (`NpgsqlException` ou `TimeoutException`)
- **ENTÃO** sistema tenta reconectar até 3 vezes com backoff exponencial
- **E** se todas as tentativas falham, registra erro e continua

#### Cenário: Cancelamento pelo usuário durante retry
- **QUANDO** usuário pressiona Ctrl+C enquanto uma query está sendo executada ou retentada
- **ENTÃO** sistema NÃO inicia nova tentativa
- **E** sistema encerra a execução imediatamente

## ADICIONADO Requirements

### Requirement: Cancelamento via Ctrl+C
O sistema DEVE interromper todas as queries em andamento quando o usuário pressiona Ctrl+C, encerrando o processo limpo sem travar.

#### Cenário: Ctrl+C durante execução de query
- **QUANDO** usuário pressiona Ctrl+C durante a execução de queries
- **ENTÃO** sistema cancela todas as queries em andamento em todos os servidores e databases
- **E** sistema exibe mensagem de cancelamento
- **E** sistema encerra com exit code 130

#### Cenário: Ctrl+C com múltiplos servidores em paralelo
- **QUANDO** usuário pressiona Ctrl+C enquanto queries estão rodando em múltiplos servidores simultaneamente
- **ENTÃO** sistema cancela todos os loops paralelos (por servidor e por database)
- **E** nenhuma nova query é iniciada após o sinal de cancelamento

#### Cenário: Ctrl+C pressionado múltiplas vezes
- **QUANDO** usuário pressiona Ctrl+C mais de uma vez
- **ENTÃO** sistema encerra da mesma forma que no primeiro Ctrl+C
- **E** não há travamento nem execução duplicada de cancelamento
