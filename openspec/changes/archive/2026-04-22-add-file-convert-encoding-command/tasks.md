## 1. Setup e Dependências

- [x] 1.1 Adicionar pacote NuGet `UTF.Unknown 2.6.0` ao projeto `FurLab.Core`
- [x] 1.2 Adicionar pacote `Microsoft.Extensions.FileSystemGlobbing` ao projeto `FurLab.Core`

## 2. Core - Interface e Modelos

- [x] 2.1 Criar interface `IEncodingConversionService` em `FurLab.Core/Interfaces/`
- [x] 2.2 Criar modelos: `EncodingConversionOptions`, `EncodingConversionResult`, `EncodingConversionProgress`
- [x] 2.3 Definir lista de extensões de texto conhecidas como constante em `TextFileExtensions`

## 3. Core - Implementação do Serviço

- [x] 3.1 Implementar `EncodingConversionService` com método `ConvertFilesAsync`
- [x] 3.2 Implementar detecção de encoding: BOM detection → UTF.Unknown
- [x] 3.3 Implementar lógica de threshold de confiança (default 0.8)
- [x] 3.4 Implementar filtro `--text-only` com lista de extensões
- [x] 3.5 Implementar glob matching recursivo para encontrar arquivos
- [x] 3.6 Implementar backup com criação de arquivos `.bak`
- [x] 3.7 Implementar escrita segura (arquivo temporário primeiro, depois rename)
- [x] 3.8 Implementar preservação de timestamps
- [x] 3.9 Implementar relatório de resultados (convertidos, ignorados, erros)
- [x] 3.10 Registrar serviço em `ServiceCollectionExtensions.cs`

## 4. CLI - Command e Settings

- [x] 4.1 Criar diretório `FurLab.CLI/Commands/Files/ConvertEncoding/`
- [x] 4.2 Criar `FilesConvertEncodingSettings.cs` com todas as opções (--from, --to, --backup, etc.)
- [x] 4.3 Criar `FilesConvertEncodingCommand.cs` herdando de `AsyncCommand<TSettings>`
- [x] 4.4 Implementar validação de caminhos via `SecurityUtils`
- [x] 4.5 Implementar progress reporting com Spectre.Console (Status/Progress)
- [x] 4.6 Implementar tratamento de exceções e códigos de saída apropriados

## 5. Testes

- [x] 5.1 Criar testes unitários para `EncodingConversionService`
- [x] 5.2 Criar testes para detecção de encoding (BOM e heurística)
- [x] 5.3 Criar testes para filtro de extensões `--text-only`
- [x] 5.4 Criar testes para backup e preservação de arquivos
- [x] 5.5 Criar testes de integração para o comando CLI

## 6. Documentação e Finalização

- [x] 6.1 Atualizar `CLAUDE.md` com o novo comando
- [x] 6.2 Verificar se todos os cenários dos specs estão cobertos
- [x] 6.3 Executar `./format.ps1` para garantir formatação correta
- [x] 6.4 Executar `dotnet test` - 137 testes passando, todos os cenários cobertos e bugs de glob matching/output directory corrigidos.
