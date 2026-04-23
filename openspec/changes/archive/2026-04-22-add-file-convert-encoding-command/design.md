## Context

O FurLab CLI atualmente não possui capacidade de converter arquivos entre encodings. O comando a ser implementado seguirá o padrão estabelecido no projeto:
- Estrutura de comandos Spectre.Console.Cli
- Separação entre CLI (FurLab.CLI) e Core (FurLab.Core)
- Uso de interfaces e injeção de dependência
- Pattern OperationResult para retornos

A detecção de encoding será feita via biblioteca UTF.Unknown, um port do Mozilla Universal Charset Detector amplamente usado (PowerToys, Jellyfin, etc.).

## Goals / Non-Goals

**Goals:**
- Implementar comando `file convert-encoding` com suporte a batch processing
- Suportar detecção automática de encoding via UTF.Unknown
- Permitir conversão in-place ou para diretório de saída
- Oferecer backup automático dos arquivos originais
- Filtrar automaticamente extensões de texto conhecidas
- Manter compatibilidade com padrões de código do projeto

**Non-Goals:**
- Suporte a arquivos binários ou detecção de tipo MIME
- Edição de arquivos in-place (load completo em memória é aceitável)
- Preservação de atributos de arquivo especiais (timestamps são mantidos)
- Detecção de encoding com 100% de precisão (é estatística)

## Decisions

### DEC-1: Uso de UTF.Unknown para detecção de encoding
**Decisão**: Adicionar dependência NuGet `UTF.Unknown 2.6.0`

**Rationale**: 
- O .NET não possui detector de encoding built-in além de BOM detection
- UTF.Unknown é o port mais maduro do Mozilla chardet para .NET
- Usado por projetos Microsoft (PowerToys) e outros grandes (Jellyfin)
- Retorna confidence score para cada detecção
- Suporta 30+ encodings sem configuração adicional

**Alternativas consideradas**:
- Heurística simples UTF-8: muito limitada, não detecta encoding específico
- Ude (deprecado): UTF.Unknown é o sucessor ativo
- Chardet.NET: menos popular e mantido

### DEC-2: Estratégia de detecção de encoding
**Decisão**: Cascata de detecção: BOM → UTF.Unknown → Erro/Warning

**Rationale**:
1. BOM detection é 100% confiável quando presente
2. UTF.Unknown analisa conteúdo e retorna confidence
3. Se confidence < threshold (default 0.8), warning ao usuário
4. `--force` permite converter mesmo com baixa confiança
5. `--from` bypassa detecção completamente

**Fluxo**:
```
Se --from especificado:
    → Usa encoding fornecido
Senão:
    → Verifica BOM (UTF-8, UTF-16LE/BE, UTF-32)
    → Se não tem BOM: UTF.Unknown.DetectFromBytes()
    → Se confidence < 0.8 e não --force: skip com warning
```

### DEC-3: Processamento batch com glob patterns
**Decisão**: Usar `Directory.EnumerateFiles` com `SearchOption.AllDirectories` combinado com glob matching via `Microsoft.Extensions.FileSystemGlobbing`

**Rationale**:
- Já usado no comando `file combine` do projeto
- Suporta patterns como `**/*.cs`, `src/**/*.txt`
- Recursivo por padrão
- Performance adequada para codebases típicos

### DEC-4: Modo in-place vs output directory
**Decisão**: In-place é o comportamento padrão; `--output <dir>` para diretório alternativo

**Rationale**:
- Para codebases, in-place é o fluxo mais comum
- `--backup` mitiga risco de perda de dados
- Com `--output`, preserva estrutura de subdiretórios

**Comportamento**:
```
Sem --output:
    arquivo.txt → converte → arquivo.txt (sobrescreve)
    + arquivo.txt.bak (se --backup)

Com --output ./converted:
    src/arquivo.txt → converte → converted/arquivo.txt
    (preserva estrutura relativa)
```

### DEC-5: Extensões de texto (--text-only)
**Decisão**: Lista embutida de extensões comuns, filtráveis via `--text-only`

**Lista de extensões**:
- C#/.NET: .cs, .cshtml, .razor, .csproj, .sln, .props, .targets
- Web: .html, .css, .js, .ts, .json, .xml
- Config: .yml, .yaml, .toml, .ini, .conf
- Script: .sh, .ps1, .cmd, .sql
- Docs: .md, .txt, .csv

**Rationale**: Evita tentar converter arquivos binários (imagens, dlls) que podem estar no glob pattern

### DEC-6: Estrutura de arquivos
**Decisão**: Seguir convenção do projeto com subpasta própria

```
FurLab.CLI/Commands/Files/ConvertEncoding/
├── FilesConvertEncodingCommand.cs
├── FilesConvertEncodingSettings.cs
└── FilesConvertEncodingConfig.cs (opcional)

FurLab.Core/
├── Interfaces/IEncodingConversionService.cs
└── Services/EncodingConversionService.cs
```

## Risks / Trade-offs

**[Risco] Detecção incorreta de encoding** → Mitigação: threshold de confiança configurável; `--force` explícito necessário; backup habilitado por padrão em conversões críticas

**[Risco] Arquivos corrompidos se conversão falhar no meio** → Mitigação: escrever em arquivo temporário primeiro, só sobrescreve original após sucesso

**[Risco] Performance em codebases muito grandes** → Mitigação: processamento paralelo via Parallel.ForEach ou async/await com throttling; progress reporting via Spectre.Console

**[Trade-off] Dependência adicional (UTF.Unknown)** → Aceitável dado o valor da detecção precisa; package bem mantido (6.8M+ downloads)

**[Trade-off] Memória vs Simplicidade** → Load completo do arquivo em byte[] para detecção UTF.Unknown; alternativa seria stream com buffer limitado, mas mais complexo; codebases de texto raramente têm arquivos >10MB

## Migration Plan

Não aplicável — é feature nova, não há migração de dados ou breaking changes.

Deploy:
1. Merge da feature
2. Update de documentação
3. Release notes mencionam novo comando

## Open Questions

1. Qual o valor padrão ideal para `--confidence`? (proposta: 0.8)
2. Devemos adicionar `--exclude` para padrões como `node_modules/**`? (sim, útil)
3. Preservar timestamp do arquivo original? (sim, implementar)
