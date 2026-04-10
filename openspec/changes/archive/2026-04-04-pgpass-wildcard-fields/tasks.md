## 1. Camada de Validação

- [x] 1.1 Atualizar `SecurityUtils.IsValidHost()` para aceitar `*` como entrada válida (retorno antecipado antes do pattern matching)
- [x] 1.2 Atualizar `SecurityUtils.IsValidPort()` para aceitar `*` como entrada válida (retorno antecipado antes da validação numérica)
- [x] 1.3 Atualizar `SecurityUtils.IsValidUsername()` para aceitar `*` como entrada válida (retorno antecipado antes do pattern matching)

## 2. Testes

- [x] 2.1 Adicionar caso de teste para `IsValidHost("*")` retornando verdadeiro
- [x] 2.2 Adicionar caso de teste para `IsValidHost(" * ")` retornando falso (rejeição de espaços)
- [x] 2.3 Adicionar caso de teste para `IsValidPort("*")` retornando verdadeiro
- [x] 2.4 Adicionar caso de teste para `IsValidPort(" * ")` retornando falso
- [x] 2.5 Adicionar caso de teste para `IsValidUsername("*")` retornando verdadeiro
- [x] 2.6 Adicionar caso de teste para `IsValidUsername(" * ")` retornando falso
- [x] 2.7 Adicionar testes de integração para `pgpass add` com opções curinga (`--host *`, `--port *`, `--username *`)
- [x] 2.8 Adicionar teste de integração para `pgpass add` com todos os curingas (`*:*:*:*:senha`)

## 3. Documentação

- [x] 3.1 Atualizar tabela de opções em `specs/011-pgpass-cli-setup/contracts/pgpass-command.md` para documentar `*` como entrada válida em `--host`, `--port` e `--username`
- [x] 3.2 Adicionar exemplo de curinga total na seção de Exemplos (`*:*:*:*:senha`)

## 4. Verificação

- [x] 4.1 Executar todos os testes para garantir ausência de regressões
- [x] 4.2 Verificar que `FurLab database pgpass add "*" --host "*" --port "*" --username "*" --password teste` cria a entrada correta
