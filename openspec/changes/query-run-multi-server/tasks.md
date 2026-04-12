## 1. Infraestrutura de Configuração (furlab.jsonc)

- [x] 1.1 Criar `UserConfigService` em FurLab.Core/Services para ler/escrever `%LocalAppData%\FurLab\furlab.jsonc`
- [x] 1.2 Adicionar dependência NuGet para parsing JSONC (ex: JsonCommentStrip ou regex simples)
- [x] 1.3 Criar modelos `ServerConfig` e `UserDefaults` em FurLab.CLI/CommandOptions/
- [x] 1.4 Implementar validação de schema para `furlab.jsonc` (campos obrigatórios, defaults)
- [x] 1.5 Implementar criação automática do arquivo se não existir
- [x] 1.6 Implementar fallback para leitura de `appsettings.json` durante transição
- [x] 1.7 Adicionar método estático facade em FurLab.CLI/Services/ConfigurationService.cs

## 2. Comando settings db-servers

- [x] 2.1 Criar `SettingsCommand.cs` com estrutura de subcomandos em FurLab.CLI/Commands/
- [x] 2.2 Implementar `settings db-servers ls` com output tabular (Spectre.Console Table)
- [x] 2.3 Implementar `settings db-servers add` com flags diretas (-n, -h, -p, -U, -W, -d, --ssl, --timeout, --command-timeout, --parallelism, --fetch-all, --exclude-patterns)
- [x] 2.4 Implementar `settings db-servers add -i` com prompts interativos sequenciais (Spectre.Console)
- [x] 2.5 Implementar `settings db-servers rm` com modo interativo (MultiSelectionPrompt) e direto (-n)
- [x] 2.6 Implementar `settings db-servers test -n <nome>` com teste de conexão e listagem de databases
- [x] 2.7 Registrar comando `settings` no Program.cs
- [x] 2.8 Adicionar validação de input (host, porta, nome único) no modo direto

## 3. Parâmetro -c e validação de query

- [x] 3.1 Adicionar opção `-c` ao `query run` em QueryCommand.cs
- [x] 3.2 Implementar validação de exclusão mútua entre `-c` e `-i`
- [x] 3.3 Implementar validação de query não vazia para `-c`
- [x] 3.4 Atualizar `QueryCommandOptions` para incluir propriedade `InlineQuery`
- [x] 3.5 Tratar escape de aspas em queries inline (Windows)

## 4. Detecção de queries destrutivas

- [x] 4.1 Criar `SqlQueryAnalyzer` em FurLab.CLI/Services/ com regex para detecção de keywords
- [x] 4.2 Implementar remoção de comentários SQL (`--`, `/* */`) antes da análise
- [x] 4.3 Implementar handling de CTEs (`WITH ... AS`) para encontrar primeiro keyword real
- [x] 4.4 Criar enum `QueryType` (Safe, Destructive) e método de classificação
- [x] 4.5 Implementar prompt de confirmação interativo para queries destrutivas (Spectre.Console)
- [x] 4.6 Exibir informações detalhadas na confirmação (tipo, servidores afetados, databases afetadas, preview da query)
- [x] 4.7 Adicionar opção `--no-confirm` para pular confirmação (uso em scripts/CI)

## 5. Fluxo interativo de seleção de servidores

- [x] 5.1 Remover flag `--servers` do `query run` em QueryCommand.cs
- [x] 5.2 Remover lógica de "servidor primário" do BuildConnectionString
- [x] 5.3 Implementar MultiSelectionPrompt para lista de servidores (todos pré-selecionados)
- [x] 5.4 Implementar mensagem quando nenhum servidor configurado (exit code 2)
- [x] 5.5 Implementar exibição de informações do servidor na lista (nome, host, porta, databases/auto-discover)
- [x] 5.6 Implementar handling de Ctrl+C durante seleção (exit code 130)
- [x] 5.7 Implementar validação quando usuário desmarca todos os servidores

## 6. Auto-descoberta de databases

- [x] 6.1 Criar método `ListDatabasesAsync` em PostgresDatabaseLister.cs
- [x] 6.2 Implementar query `SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true`
- [x] 6.3 Implementar filtragem por `excludePatterns` com suporte a wildcard `*`
- [x] 6.4 Integrar auto-descoberta no fluxo de execução quando `fetchAllDatabases: true`
- [x] 6.5 Implementar fallback para databases configuradas explicitamente se auto-descoberta falhar
- [x] 6.6 Implementar validação de acesso a cada database descoberta antes de executar query

## 7. Execução paralela e tolerância a falhas

- [x] 7.1 Refatorar loop de execução para usar `Parallel.ForEachAsync`
- [x] 7.2 Implementar `MaxDegreeOfParallelism` configurável por servidor (default 4)
- [x] 7.3 Implementar tratamento de erro individual por servidor/database (continua no próximo)
- [x] 7.4 Integrar Polly para retries automáticos em falhas transitórias (3 tentativas com backoff exponencial)
- [x] 7.5 Implementar coleta de resultados e erros para CSV consolidado
- [x] 7.6 Implementar resumo de execução pós-processamento (sucessos, falhas, erros)

## 8. CSV com identificação de servidor

- [x] 8.1 Atualizar formato de CSV para colunas: Server, Database, <colunas da query> (sem metadados de execução)
- [x] 8.2 Mover metadados de execução (ExecutedAt, Status, RowCount, Error) para log no terminal
- [x] 8.3 Falhas não geram linhas no CSV — apenas log no terminal com `✗` e tabela resumo
- [x] 8.4 Implementar output path default usando `outputDirectory` das configurações
- [x] 8.5 `--separate-files` gera 1 CSV por servidor (nome `<server>_<timestamp>.csv`) com colunas Server, Database, <query cols>
- [x] 8.6 Implementar criação de diretórios pai se não existirem
- [x] 8.7 Adicionar log de execução por database no terminal (`✓/✗ server/db — Status — rows/error (timestamp)`)
- [x] 8.8 Adicionar tabela Spectre.Console no resumo final (Server, Database, Status, Rows, ExecutedAt, Error)

## 9. Integração e refatoração do QueryCommand.Run

- [x] 9.1 Refatorar método `Run()` para novo fluxo: query → seleção servidores → confirmação → execução paralela → CSV
- [x] 9.2 Remover métodos obsoletos (`RunOnAllServers`, lógica de servidor primário)
- [x] 9.3 Atualizar `QueryCommandOptions` para remover propriedades obsoletas
- [x] 9.4 Integrar todos os componentes (config, seleção, detecção, execução paralela, CSV)
- [x] 9.5 Atualizar mensagens de erro e help text para refletir novo comportamento

## 10. Testes e validação

- [ ] 10.1 Criar testes unitários para `SqlQueryAnalyzer` (keywords destrutivas, comentários, CTEs)
- [ ] 10.2 Criar testes unitários para `UserConfigService` (leitura, escrita, validação, fallback)
- [ ] 10.3 Criar testes de integração para `settings db-servers` (add, ls, rm, test)
- [ ] 10.4 Criar testes de integração para `query run` com múltiplos servidores
- [ ] 10.5 Testar execução paralela com limite configurável
- [ ] 10.6 Testar tolerância a falhas parcial (um servidor falha, outros continuam)
- [ ] 10.7 Testar formato de CSV consolidado (Server, Database, <query cols>) e CSV por servidor (--separate-files)
- [ ] 10.8 Testar migração de `appsettings.json` para `furlab.jsonc`

## 11. Documentação

- [ ] 11.1 Atualizar README.md com novo fluxo de `query run`
- [ ] 11.2 Atualizar README.pt-BR.md com novo fluxo
- [ ] 11.3 Criar exemplo de `furlab.jsonc` com comentários explicativos
- [ ] 11.4 Documentar comandos `settings db-servers` no README
- [ ] 11.5 Documentar guard rail de queries destrutivas
- [ ] 11.6 Atualizar appsettings.example.json com nota de depreciação
