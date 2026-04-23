## Por que

O FurLab precisa de um comando para converter arquivos de texto entre diferentes encodings. O caso de uso principal é a migração de codebases legados para UTF-8, um problema comum quando se trabalha com projetos antigos que usam encodings regionais como Latin-1 (ISO-8859-1) ou Windows-1252. Atualmente, não há uma ferramenta integrada no CLI para realizar essa conversão em lote com detecção automática de encoding.

## O que Muda

- **ADICIONADO**: Novo comando `file convert-encoding` para conversão de encodings
- **ADICIONADO**: Suporte a batch processing com padrões glob recursivos (`**/*.cs`)
- **ADICIONADO**: Detecção automática de encoding usando a biblioteca UTF.Unknown (port do Mozilla Universal Charset Detector)
- **ADICIONADO**: Opção `--backup` para criar arquivos `.bak` antes da conversão
- **ADICIONADO**: Modo `--text-only` que filtra automaticamente por extensões de texto conhecidas
- **ADICIONADO**: Conversão in-place por padrão, com opção de diretório de saída via `--output`
- **ADICIONADO**: Parâmetro opcional `--from` para especificar encoding origem (auto-detect se omitido)
- **ADICIONADO**: Parâmetro obrigatório `--to` para especificar encoding destino
- **ADICIONADO**: Suporte a 30+ encodings via UTF.Unknown (UTF-8, UTF-16, ISO-8859-x, Windows-125x, etc.)
- **ADICIONADO**: Threshold de confiança configurável para detecção automática

## Capacidades

### Novas Capacidades
- `file-convert-encoding`: Conversão de arquivos de texto entre encodings com suporte a batch processing, detecção automática e backup

### Capacidades Modificadas
- *(Nenhuma capacidade existente será modificada)*

## Impacto

- **Dependências**: Nova dependência NuGet `UTF.Unknown 2.6.0` para detecção de encoding
- **CLI**: Novo comando `file convert-encoding` disponível no FurLab.CLI
- **Core**: Novo serviço `IEncodingConversionService` e sua implementação em FurLab.Core
- **API**: Interface pública adicionada, sem breaking changes
- **Performance**: Processamento stream-based para arquivos grandes
- **Segurança**: Validação de caminhos via `SecurityUtils`, prevenção de path traversal
