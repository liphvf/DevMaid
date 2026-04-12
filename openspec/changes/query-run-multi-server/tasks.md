## 1. Infraestrutura de Configuração (furlab.jsonc)

- [ ] 1.1 Criar `UserConfigService` em FurLab.Core/Services para ler/escrever `%LocalAppData%\FurLab\furlab.jsonc`
- [ ] 1.2 Adicionar dependência NuGet para parsing JSONC (ex: JsonCommentStrip ou regex simples)
- [ ] 1.3 Criar modelos `ServerConfig` e `UserDefaults` em FurLab.CLI/CommandOptions/
- [ ] 1.4 Implementar validação de schema para `furlab.jsonc` (campos obrigatórios, defaults)
- [ ] 1.5 Implementar criação automática do arquivo se não existir
- [ ] 1.6 Implementar fallback para leitura de `appsettings.json` durante transição
- [ ] 1.7 Adicionar método estático facade em FurLab.CLI/Services/ConfigurationService.cs

## 2. Comando settings db-servers

- [ ] 2.1 Criar `SettingsCommand.cs` com estrutura de subcomandos em FurLab.CLI/Commands/
- [ ] 2.2 Implementar `settings db-servers ls` com output tabular (Spectre.Console Table)
- [ ] 2.3 Implementar `settings db-servers add` com flags diretas (-n, -h, -p, -U, -W, -d, --ssl, --timeout, --command-timeout, --parallelism, --fetch-all, --exclude-patterns)
- [ ] 2.4 Implementar `settings db-servers add -i` com prompts interativos sequenciais (Spectre.Console)
- [ ] 2.5 Implementar `settings db-servers rm` com modo interativo (MultiSelectionPrompt) e direto (-n)
- [ ] 2.6 Implementar `settings db-servers test -n <nome>` com teste de conexão e listagem de databases
- [ ] 2.7 Registrar comando `settings` no Program.cs
- [ ] 2.8 Adicionar validação de input (host, porta, nome único) no modo direto

## 3. Parâmetro -c e validação de query

- [ ] 3.1 Adicionar opção `-c` ao `query run` em QueryCommand.cs
- [ ] 3.2 Implementar validação de exclusão mútua entre `-c` e `-i`
- [ ] 3.3 Implementar validação de query não vazia para `-c`
- [ ] 3.4 Atualizar `QueryCommandOptions` para incluir propriedade `InlineQuery`
- [ ] 3.5 Atar tratamento de escape de aspas em queries inline (Windows)

## 4. Detecção de queries destrutivas

- [ ] 4.1 Criar `SqlQueryAnalyzer` em FurLab.CLI/Services/ com regex para detecção de keywords
- [ ] 4.2 Implementar remoção de comentários SQL (`--`, `/* */`) antes da análise
- [ ] 4.3 Implementar handling de CTEs (`WITH ... AS`) para encontrar primeiro keyword real
- [ ] 4.4 Criar enum `QueryType` (Safe, Destructive) e método de classificação
- [ ] 4.5 Implementar prompt de confirmação interativo para queries destrutivas (Spectre.Console)
- [ ] 4.6 Exibir informações detalhadas na confirmação (tipo, servidores afetados, databases afetadas, preview da query)
- [ ] 4.7 Adicionar opção `--no-confirm` para pular confirmação (uso em scripts/CI)

## 5. Fluxo interativo de seleção de servidores

- [ ] 5.1 Remover flag `--servers` do `query run` em QueryCommand.cs
- [ ] 5.2 Remover lógica de "servidor primário" do BuildConnectionString
- [ ] 5.3 Implementar MultiSelectionPrompt para lista de servidores (todos pré-selecionados)
- [ ] 5.4 Implementar mensagem quando nenhum servidor configurado (exit code 2)
- [ ] 5.5 Implementar exibição de informações do servidor na lista (nome, host, porta, databases/auto-discover)
- [ ] 5.6 Implementar handling de Ctrl+C durante seleção (exit code 130)
- [ ] 5.7 Implementar validação quando usuário desmarca todos os servidores

## 6. Auto-descoberta de databases

- [ ] 6.1 Criar método `ListDatabasesAsync` em PostgresDatabaseLister.cs
- [ ] 6.2 Implementar query `SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true`
- [ ] 6.3 Implementar filtragem por `excludePatterns` com suporte a wildcard `*`
- [ ] 6.4 Integrar auto-descoberta no fluxo de execução quando `fetchAllDatabases: true`
- [ ] 6.5 Implementar fallback para databases configuradas explicitamente se auto-descoberta falhar
- [ ] 6.6 Implementar validação de acesso a cada database descoberta antes de executar query

## 7. Execução paralela e tolerância a falhas

- [ ] 7.1 Refatorar loop de execução para usar `Parallel.ForEachAsync`
- [ ] 7.2 Implementar `MaxDegreeOfParallelism` configurável por servidor (default 4)
- [ ] 7.3 Implementar tratamento de erro individual por servidor/database (continua no próximo)
- [ ] 7.4 Integrar Polly para retries automáticos em falhas transitórias (3 tentativas com backoff exponencial)
- [ ] 7.5 Implementar coleta de resultados e erros para CSV consolidado
- [ ] 7.6 Implementar resumo de execução pós-processamento (sucessos, falhas, erros)

## 8. CSV consolidado com metadados

- [ ] 8.1 Atualizar formato de CSV para incluir colunas: Server, Database, ExecutedAt, Status, RowCount, Error
- [ ] 8.2 Implementar geração de timestamp ISO 8601 para ExecutedAt
- [ ] 8.3 Implementar linhas de erro no CSV (Status=Error, RowCount vazio, Error preenchido)
- [ ] 8.4 Implementar output path default usando `outputDirectory` das configurações
- [ ] 8.5 Manter suporte a `--separate-files` (um CSV por database com nome `<server>_<database>_<timestamp>.csv`)
- [ ] 8.6 Implementar criação de diretórios pai se não existirem

## 9. Integração e refatoração do QueryCommand.Run

- [ ] 9.1 Refatorar método `Run()` para novo fluxo: query → seleção servidores → confirmação → execução paralela → CSV
- [ ] 9.2 Remover métodos obsoletos (`RunOnAllServers`, lógica de servidor primário)
- [ ] 9.3 Atualizar `QueryCommandOptions` para remover propriedades obsoletas
- [ ] 9.4 Integrar todos os componentes (config, seleção, detecção, execução paralela, CSV)
- [ ] 9.5 Atualizar mensagens de erro e help text para refletir novo comportamento

## 10. Testes e validação

- [ ] 10.1 Criar testes unitários para `SqlQueryAnalyzer` (keywords destrutivas, comentários, CTEs)
- [ ] 10.2 Criar testes unitários para `UserConfigService` (leitura, escrita, validação, fallback)
- [ ] 10.3 Criar testes de integração para `settings db-servers` (add, ls, rm, test)
- [ ] 10.4 Criar testes de integração para `query run` com múltiplos servidores
- [ ] 10.5 Testar execução paralela com limite configurável
- [ ] 10.6 Testar tolerância a falhas parcial (um servidor falha, outros continuam)
- [ ] 10.7 Testar formato de CSV consolidado com metadados
- [ ] 10.8 Testar migração de `appsettings.json` para `furlab.jsonc`

## 11. Documentação

- [ ] 11.1 Atualizar README.md com novo fluxo de `query run`
- [ ] 11.2 Atualizar README.pt-BR.md com novo fluxo
- [ ] 11.3 Criar exemplo de `furlab.jsonc` com comentários explicativos
- [ ] 11.4 Documentar comandos `settings db-servers` no README
- [ ] 11.5 Documentar guard rail de queries destrutivas
- [ ] 11.6 Atualizar appsettings.example.json com nota de depreciação
