# Spec: query-run-multi-server-execution

## Purpose

Define o comportamento de execução paralela de queries em múltiplos servidores/databases, tolerância a falhas parciais, retry automático com Polly e exibição de resumo de execução.

## Requirements

### Requirement: Execução paralela em múltiplos servidores
O sistema DEVE executar queries em múltiplos servidores/databases em paralelo usando `Parallel.ForEachAsync` com grau de paralelismo configurável.

#### Cenário: Execução paralela com limite padrão
- **QUANDO** usuário seleciona múltiplos servidores e databases
- **ENTÃO** sistema executa queries em paralelo com no máximo 4 conexões simultâneas (default)

#### Cenário: Execução paralela com limite customizado
- **QUANDO** servidor tem `maxParallelism` configurado no `furlab.jsonc`
- **ENTÃO** sistema usa o valor configurado como limite de paralelismo para aquele servidor

### Requirement: Tolerância a falhas parcial
O sistema DEVE continuar a execução nos servidores/databases restantes quando um falha, registrando o erro no terminal e no arquivo de erros `_erros.csv`.

#### Cenário: Um servidor falha durante execução
- **QUANDO** um servidor retorna erro de conexão durante a execução
- **ENTÃO** sistema registra erro no feed de atividades com `✗ server/db — Error — <mensagem>`
- **E** sistema adiciona linha ao arquivo `results/<timestamp>/<timestamp>_erros.csv`
- **E** erro NÃO gera linha nos CSVs de resultado
- **E** sistema continua executando nos servidores restantes

#### Cenário: Múltiplos servidores falham
- **QUANDO** múltiplos servidores retornam erros durante a execução
- **ENTÃO** sistema registra cada erro individualmente no feed de atividades
- **E** sistema adiciona cada erro progressivamente ao arquivo `_erros.csv`
- **E** sistema continua com os servidores que estão funcionando

#### Cenário: Todos os servidores falham
- **QUANDO** todos os servidores selecionados retornam erro durante a execução
- **ENTÃO** sistema exibe resumo com todos os erros
- **E** arquivo `_erros.csv` contém todas as falhas
- **E** sistema exibe mensagem: "Nenhum servidor respondeu com sucesso. Verifique as conexões."
- **E** sistema encerra com exit code 1

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

### Requirement: Resumo de execução
O sistema DEVE exibir um resumo após a execução em todos os servidores/databases, incluindo caminhos dos arquivos gerados.

#### Cenário: Execução completa com sucesso parcial
- **QUANDO** execução termina em todos os servidores
- **ENTÃO** sistema exibe caminho do CSV consolidado: `✅ Consolidated → <caminho>`
- **E** sistema exibe caminho do arquivo de erros (se houver): `❌ Errors → <caminho>`
- **E** sistema exibe contagem: "X servidores | Y success | Z failed | N total rows"
