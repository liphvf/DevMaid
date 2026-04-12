## Por que

O comando `query run` atualmente depende de edição manual do `appsettings.json` para configurar servidores e exige que o usuário sempre passe parâmetros de conexão explicitamente. Isso torna o fluxo de trabalho lento e propenso a erros, especialmente quando se precisa executar a mesma query em múltiplos servidores. Além disso, não há um comando CLI para gerenciar servidores configurados - tudo é feito editando arquivos JSON manualmente.

## O que Muda

- **Configuração migrada para `%LocalAppData%\FurLab\furlab.jsonc`**: O arquivo `appsettings.json` do projeto deixa de ser usado para configuração de servidores. As configurações passam a ser por usuário, não por projeto.
- **`query run` sem parâmetros de servidor**: Remove a dependência de "servidor primário". Sempre que o comando é executado, uma lista interativa de servidores configurados é apresentada para seleção múltipla.
- **Novo parâmetro `-c`**: Permite passar a query diretamente na linha de comando, como `psql -c`. Mutuamente exclusivo com `-i`.
- **Guard rail para queries destrutivas**: Detecção via regex de comandos que modificam dados (INSERT, UPDATE, DELETE, ALTER, DROP, CREATE, TRUNCATE, MERGE, GRANT, REVOKE). Exibe confirmação antes de executar.
- **Execução paralela configurável**: Queries em múltiplos servidores/databases rodam em paralelo com limite configurável (default: 4).
- **CSV consolidado com metadados**: Output inclui colunas Server, Database, ExecutedAt, Status, RowCount, Error além das colunas de resultado.
- **Tolerância a falhas parcial**: Se um servidor falha, o erro é reportado e a execução continua nos próximos.
- **Novo comando `settings db-servers`**: CRUD completo para gerenciar servidores configurados.
  - `ls`: lista servidores em formato tabular
  - `add`: adiciona servidor (modo interativo `-i` ou direto com flags)
  - `rm`: remove servidor (interativo ou por nome)
  - `test`: testa conexão com servidor
- **Auto-descoberta de databases**: Flag `fetchAllDatabases` por servidor executa query em todas as databases do servidor (excluindo templates e patterns configuráveis).
- **Remoção do flag `--servers`**: Não faz mais sentido com o novo fluxo interativo.

## Capacidades

### Novas Capacidades
- `query-run-interactive`: Fluxo interativo de seleção de servidores via MultiSelectionPrompt quando nenhum servidor é especificado na linha de comando.
- `query-run-inline`: Suporte ao parâmetro `-c` para passar query diretamente na linha de comando, mutuamente exclusivo com `-i`.
- `query-run-multi-server-execution`: Execução paralela de queries em múltiplos servidores/databases com tolerância a falhas parcial e CSV consolidado com metadados.
- `query-run-destructive-detection`: Detecção e confirmação de queries destrutivas via regex antes da execução.
- `settings-db-servers`: Comando CLI para gerenciar servidores configurados (add, ls, rm, test) com modo interativo e direto.
- `settings-user-config`: Armazenamento de configurações em `%LocalAppData%\FurLab\furlab.jsonc` com suporte a JSONC (comentários).
- `server-auto-discover-databases`: Auto-descoberta de databases por servidor via query `pg_database` com patterns de exclusão configuráveis.

### Capacidades Modificadas
- `query-run-csv-export`: **MODIFICADO** - Formato do CSV muda para incluir colunas Server, Database, ExecutedAt, Status, RowCount, Error.

## Impacto

- **FurLab.CLI/Program.cs**: Registro do novo comando `settings` e remoção do flag `--servers` do `query run`.
- **FurLab.CLI/Commands/QueryCommand.cs**: Refatoração significativa do fluxo de execução, remoção de `--servers`, adição de `-c`, guard rail de detecção, execução paralela, novo formato de CSV.
- **FurLab.CLI/Commands/SettingsCommand.cs**: Novo arquivo com subcomandos `db-servers ls/add/rm/test`.
- **FurLab.CLI/CommandOptions/**: Novos DTOs para configurações de servidor e opções do comando settings.
- **FurLab.Core/Services/ConfigurationService.cs**: Migração de `appsettings.json` para `furlab.jsonc` em `%LocalAppData%\FurLab\`.
- **appsettings.json**: Deixa de ser usado para configuração de servidores (breaking change para quem usa `--servers` ou servidor primário).
- **Documentação**: README e exemplos precisam ser atualizados para refletir o novo fluxo.
