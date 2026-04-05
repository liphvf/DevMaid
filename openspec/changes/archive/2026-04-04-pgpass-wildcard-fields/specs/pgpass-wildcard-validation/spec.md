## Requisitos ADICIONADOS

### Requisito: Validação de curinga no hostname

A função `SecurityUtils.IsValidHost()` DEVE aceitar a string literal `*` como hostname válido, retornando `true` sem realizar pattern matching adicional.

#### Cenário: Curinga no hostname aceito
- **QUANDO** `IsValidHost("*")` é chamado
- **ENTÃO** a função retorna `true`

#### Cenário: Curinga com espaços rejeitado
- **QUANDO** `IsValidHost(" * ")` ou `IsValidHost("* ")` é chamado
- **ENTÃO** a função retorna `false`

### Requisito: Validação de curinga na porta

A função `SecurityUtils.IsValidPort()` DEVE aceitar a string literal `*` como porta válida, retornando `true` sem realizar validação numérica.

#### Cenário: Curinga na porta aceito
- **QUANDO** `IsValidPort("*")` é chamado
- **ENTÃO** a função retorna `true`

#### Cenário: Curinga com espaços rejeitado
- **QUANDO** `IsValidPort(" * ")` ou `IsValidPort("* ")` é chamado
- **ENTÃO** a função retorna `false`

#### Cenário: Portas numéricas ainda validadas
- **QUANDO** `IsValidPort("5432")` é chamado
- **ENTÃO** a função retorna `true` (comportamento existente preservado)

#### Cenário: Portas inválidas ainda rejeitadas
- **QUANDO** `IsValidPort("-1")` ou `IsValidPort("99999")` é chamado
- **ENTÃO** a função retorna `false` (comportamento existente preservado)

### Requisito: Validação de curinga no usuário

A função `SecurityUtils.IsValidUsername()` DEVE aceitar a string literal `*` como usuário válido, retornando `true` sem realizar pattern matching adicional.

#### Cenário: Curinga no usuário aceito
- **QUANDO** `IsValidUsername("*")` é chamado
- **ENTÃO** a função retorna `true`

#### Cenário: Curinga com espaços rejeitado
- **QUANDO** `IsValidUsername(" * ")` ou `IsValidUsername("* ")` é chamado
- **ENTÃO** a função retorna `false`

### Requisito: Sem alterações na validação da senha

Campos de senha NÃO DEVEM aceitar valores curinga. O requisito de senha permanece inalterado.

#### Cenário: Curinga em senha não é aplicável
- **QUANDO** o usuário fornece a senha via flag `--password` ou prompt interativo
- **ENTÃO** o valor é armazenado como informado; o comportamento de curinga não se aplica

### Requisito: PgPassCommand aceita curingas nas opções

O comando `devmaid database pgpass add` DEVE aceitar `*` como entrada válida para as opções `--host`, `--port` e `--username`.

#### Cenário: Adicionar entrada com curinga no host
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --host "*" --password senha`
- **ENTÃO** a entrada é adicionada com `*:5432:meu_banco:postgres:senha`

#### Cenário: Adicionar entrada com curinga na porta
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --port "*" --password senha`
- **ENTÃO** a entrada é adicionada com `localhost:*:meu_banco:postgres:senha`

#### Cenário: Adicionar entrada com curinga no usuário
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --username "*" --password senha`
- **ENTÃO** a entrada é adicionada com `localhost:5432:meu_banco:*:senha`

#### Cenário: Adicionar entrada com todos os curingas
- **QUANDO** o usuário executa `devmaid database pgpass add "*" --host "*" --port "*" --username "*" --password senha`
- **ENTÃO** a entrada é adicionada com `*:*:*:*:senha`
