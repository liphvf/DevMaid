## ADICIONADO Requisitos

### Requisito: Progress bar com total de databases
O sistema DEVE exibir uma progress bar mostrando quantas databases foram processadas do total.

#### Cenário: Início da execução
- **QUANDO** execução de queries inicia
- **ENTÃO** sistema exibe: "Executing query across X servers, Y databases..."
- **E** sistema exibe progress bar com 0/Y completado

#### Cenário: Database completa processamento
- **QUANDO** uma query completa (sucesso ou falha)
- **ENTÃO** progress bar incrementa em 1
- **E** porcentagem é atualizada

#### Cenário: Todas as databases completam
- **QUANDO** todas as queries terminam
- **ENTÃO** progress bar exibe 100%
- **E** progress bar desaparece após conclusão

### Requisito: Feed de atividades em tempo real
O sistema DEVE exibir um feed de atividades mostrando as queries mais recentemente completadas enquanto a execução ocorre.

#### Cenário: Query completa com sucesso
- **QUANDO** uma query executa com sucesso em server/database
- **ENTÃO** sistema exibe: `✓ server/database — N rows — → csv` em verde
- **E** caminho do CSV parcial é indicado

#### Cenário: Query falha
- **QUANDO** uma query falha em server/database
- **ENTÃO** sistema exibe: `✗ server/database — Error — <mensagem>` em vermelho

#### Cenário: Query em execução
- **QUANDO** uma query está sendo executada
- **ENTÃO** feed pode exibir indicador de progresso (e.g., `⟳ server/database running... (Ns)`)

### Requisito: Resumo final
O sistema DEVE exibir resumo após conclusão de todas as queries.

#### Cenário: Execução completa
- **QUANDO** todas as queries terminam
- **ENTÃO** sistema exibe caminho do CSV consolidado
- **E** sistema exibe caminho do arquivo de erros (se houver erros)
- **E** sistema exibe contagem: X servidores | Y success | Z failed | N total rows