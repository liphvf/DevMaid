## ADDED Requirements

### Requisito: Conversão de encoding via linha de comando
O sistema DEVE fornecer um comando CLI para converter arquivos de texto entre diferentes encodings.

#### Cenário: Conversão simples com encoding especificado
- **QUANDO** o usuário executa `file convert-encoding arquivo.txt --from Latin1 --to UTF-8`
- **ENTÃO** o arquivo é convertido de Latin1 para UTF-8
- **E** o arquivo original é sobrescrito (in-place)

#### Cenário: Conversão com detecção automática
- **QUANDO** o usuário executa `file convert-encoding arquivo.txt --to UTF-8`
- **ENTÃO** o sistema detecta automaticamente o encoding do arquivo
- **E** converte o arquivo para UTF-8

### Requisito: Batch processing com glob patterns
O sistema DEVE suportar conversão em lote usando padrões glob, incluindo recursão em subdiretórios.

#### Cenário: Conversão recursiva de arquivos C#
- **QUANDO** o usuário executa `file convert-encoding -i "**/*.cs" --to UTF-8`
- **ENTÃO** todos os arquivos .cs em todos os subdiretórios são convertidos
- **E** um relatório de progresso é exibido

#### Cenário: Conversão com filtro de extensões
- **QUANDO** o usuário executa `file convert-encoding -i "**/*" --text-only --to UTF-8`
- **ENTÃO** apenas arquivos com extensões de texto conhecidas são processados
- **E** arquivos binários são ignorados automaticamente

### Requisito: Backup de arquivos originais
O sistema DEVE suportar criação de backup dos arquivos originais antes da conversão.

#### Cenário: Conversão com backup habilitado
- **QUANDO** o usuário executa `file convert-encoding arquivo.txt --to UTF-8 --backup`
- **ENTÃO** um arquivo `.bak` é criado com o conteúdo original
- **E** o arquivo original é convertido

### Requisito: Diretório de saída opcional
O sistema DEVE permitir especificar um diretório de saída alternativo.

#### Cenário: Conversão para diretório diferente
- **QUANDO** o usuário executa `file convert-encoding -i "src/**/*.txt" --to UTF-8 -o "converted/"`
- **ENTÃO** os arquivos convertidos são salvos em `converted/` preservando a estrutura de subdiretórios
- **E** os arquivos originais permanecem inalterados

### Requisito: Detecção de encoding confiável
O sistema DEVE detectar o encoding dos arquivos usando algoritmos confiáveis e reportar nível de confiança.

#### Cenário: Detecção via BOM
- **QUANDO** um arquivo possui BOM (Byte Order Mark)
- **ENTÃO** o encoding é detectado com 100% de confiança via BOM
- **E** a conversão prossegue normalmente

#### Cenário: Detecção com baixa confiança
- **QUANDO** a detecção automática retorna confiança abaixo do threshold (0.8)
- **E** o usuário não especificou `--force`
- **ENTÃO** o arquivo é ignorado com um warning
- **E** o motivo é reportado no relatório final

#### Cenário: Forçar conversão com baixa confiança
- **QUANDO** a detecção retorna baixa confiança
- **E** o usuário executa com `--force`
- **ENTÃO** o arquivo é convertido mesmo assim
- **E** um aviso é emitido informando o risco

### Requisito: Relatório de progresso e resultados
O sistema DEVE exibir progresso durante o processamento e um relatório final.

#### Cenário: Relatório de batch processing
- **QUANDO** a conversão em lote é concluída
- **ENTÃO** um relatório é exibido com:
  - Total de arquivos processados
  - Arquivos convertidos com sucesso
  - Arquivos ignorados (já em encoding destino)
  - Arquivos com erro (detecção falhou, permissão negada, etc.)

### Requisito: Suporte a múltiplos encodings
O sistema DEVE suportar conversão entre os principais encodings.

#### Cenário: Conversão entre encodings comuns
- **QUANDO** o usuário especifica `--from` ou `--to` com valores: UTF-8, UTF-8-BOM, UTF-16, UTF-16-BE, Latin1, Windows-1252
- **ENTÃO** a conversão é realizada corretamente

### Requisito: Validação de segurança
O sistema DEVE validar caminhos de arquivo para prevenir path traversal.

#### Cenário: Tentativa de path traversal
- **QUANDO** um caminho de arquivo contém sequências como `../` ou `./`
- **ENTÃO** o caminho é validado via `SecurityUtils`
- **E** a operação é abortada se for detectado path traversal
