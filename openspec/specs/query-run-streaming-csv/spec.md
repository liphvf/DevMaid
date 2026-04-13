# Spec: query-run-streaming-csv

## Purpose

Define o comportamento de escrita progressiva de CSVs parciais por servidor, geração de CSV consolidado por merge ao final, organização em subpasta por execução, e uso de Channel-based single writer para coordenar I/O entre produtoras paralelas.

## Requirements

### Requirement: Escrita progressiva de CSV parcial por servidor
O sistema DEVE escrever em um arquivo CSV por servidor, fazendo append progressivo imediatamente após cada query completar com sucesso naquele servidor. Cada escrita DEVE fazer flush para o disco, garantindo durabilidade em caso de crash.

#### Cenário: Primeira query completa com sucesso em um servidor
- **QUANDO** uma query executa com sucesso como primeira em um servidor
- **ENTÃO** sistema cria arquivo `results/<timestamp>/<server>_<timestamp>.csv` escrevendo header + dados
- **E** arquivo contém colunas: Server, Database, <colunas de resultado da query>
- **E** cada linha de resultado é precedida pelos valores de Server e Database

#### Cenário: Query subsequente completa no mesmo servidor
- **QUANDO** uma query executa com sucesso no mesmo servidor (database diferente)
- **ENTÃO** sistema faz append dos dados no arquivo CSV daquele servidor
- **E** header NÃO é reescrito (dados são apenas adicionados ao final)
- **E** se a query traz colunas diferentes do header existente, o header fica inconsistente com relação às linhas novas — isso é ACEITÁVEL

#### Cenário: Nomes de servidor com caracteres especiais
- **QUANDO** nome de servidor contém caracteres inválidos para filename
- **ENTÃO** sistema substitui caracteres inválidos por `_`
- **E** arquivo é escrito com o nome sanitizado

#### Cenário: Durabilidade de escrita (flush)
- **QUANDO** sistema escreve dados em um CSV parcial (append)
- **ENTÃO** sistema faz flush para o disco após cada escrita
- **E** dados ficam persistidos mesmo se o processo crashar imediatamente após

### Requirement: CSV consolidado por merge
O sistema DEVE gerar um arquivo CSV consolidado após todas as queries terminarem, fazendo merge dos CSVs parciais.

#### Cenário: Múltiplas queries com sucesso
- **QUANDO** todas as queries terminam com pelo menos um sucesso
- **ENTÃO** sistema lê todos os CSVs parciais
- **E** sistema escreve `results/<timestamp>/consolidated_<timestamp>.csv`
- **E** colunas são a união de todas as colunas de resultado, na ordem de primeira aparição
- **E** cada linha inclui Server, Database e valores para todas as colunas (vazio se não existir)

#### Cenário: Apenas um servidor executado
- **QUANDO** apenas um servidor é selecionado e executa com sucesso
- **ENTÃO** sistema gera CSV parcial para aquele servidor
- **E** sistema gera CSV consolidado com os dados do CSV parcial

#### Cenário: Processo crasha antes do merge
- **QUANDO** processo termina inesperadamente antes de gerar o consolidado
- **ENTÃO** CSVs parciais já salvos permanecem disponíveis na subpasta
- **E** usuário pode acessar resultados parciais individualmente

### Requirement: Organização em subpasta por execução
O sistema DEVE organizar todos os arquivos de uma execução em uma subpasta nomeada com o timestamp.

#### Cenário: Execução com output padrão
- **QUANDO** usuário executa query run sem `-o`
- **ENTÃO** sistema cria subpasta `results/<timestamp>/`
- **E** todos os CSVs parciais, consolidado e erros ficam dentro desta subpasta

#### Cenário: Execução com output customizado
- **QUANDO** usuário fornece `-o /caminho/custom/`
- **ENTÃO** sistema cria subpasta `/caminho/custom/<timestamp>/`
- **E** todos os arquivos ficam dentro desta subpasta

### Requirement: Channel-based single writer
O sistema DEVE usar `System.Threading.Channels.Channel<CsvRow>` para coordenar escrita de arquivos entre produtoras paralelas e consumidora dedicada.

#### Cenário: Múltiplas queries completam simultaneamente
- **QUANDO** múltiplas queries completam ao mesmo tempo
- **ENTÃO** cada uma envia resultado ao Channel sem bloquear outras
- **E** consumidora dedicada escreve arquivos sequencialmente sem lock de I/O

#### Cenário: Backpressure quando escritora é lenta
- **QUANDO** Channel bounded atinge capacidade máxima
- **ENTÃO** produtores aguardam até que espaço seja liberado
- **E** nenhuma query é perdida devido a backpressure
