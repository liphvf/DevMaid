## Requisitos MODIFICADOS

### Requisito: Opção --host aceita curinga

A opção `--host` DEVE aceitar `*` como valor curinga que corresponde a qualquer hostname.

#### Cenário: Adicionar entrada com curinga no host
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --host "*" --password senha`
- **ENTÃO** a entrada é adicionada com hostname `*` no arquivo pgpass.conf

#### Cenário: Remover entrada com curinga no host
- **QUANDO** o usuário executa `devmaid database pgpass remove meu_banco --host "*"`
- **ENTÃO** a entrada correspondente a `*:5432:meu_banco:postgres` é removida

### Requisito: Opção --port aceita curinga

A opção `--port` DEVE aceitar `*` como valor curinga que corresponde a qualquer porta.

#### Cenário: Adicionar entrada com curinga na porta
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --port "*" --password senha`
- **ENTÃO** a entrada é adicionada com porta `*` no arquivo pgpass.conf

### Requisito: Opção --username aceita curinga

A opção `--username` DEVE aceitar `*` como valor curinga que corresponde a qualquer usuário.

#### Cenário: Adicionar entrada com curinga no usuário
- **QUANDO** o usuário executa `devmaid database pgpass add meu_banco --username "*" --password senha`
- **ENTÃO** a entrada é adicionada com usuário `*` no arquivo pgpass.conf

### Requisito: Documentação do contrato reflete suporte a curingas

A tabela de opções em `pgpass-command.md` DEVE ser atualizada para documentar o suporte a curingas:

| Opção | Descrição (atualizada) |
|-------|------------------------|
| `--host` | Hostname ou `*` para qualquer host. Padrão: `localhost` |
| `--port` | Porta TCP ou `*` para qualquer porta. Padrão: `5432` |
| `--username` | Nome de usuário ou `*` para qualquer usuário. Padrão: `postgres` |

#### Cenário: Documentação atualizada para a opção --host
- **QUANDO** o usuário lê a documentação do contrato
- **ENTÃO** a descrição da opção `--host` inclui `*` como entrada válida

#### Cenário: Documentação atualizada para a opção --port
- **QUANDO** o usuário lê a documentação do contrato
- **ENTÃO** a descrição da opção `--port` inclui `*` como entrada válida

#### Cenário: Documentação atualizada para a opção --username
- **QUANDO** o usuário lê a documentação do contrato
- **ENTÃO** a descrição da opção `--username` inclui `*` como entrada válida

### Requisito: Seção de exemplos inclui uso com curingas

A seção de Exemplos DEVE incluir um exemplo de curinga total demonstrando uma entrada com todos os campos como curinga:

```bash
# Entrada curinga total: qualquer host, porta, banco e usuário
devmaid database pgpass add "*" --host "*" --port "*" --username "*" --password senhapadrao
# Resultado: *:*:*:*:senhapadrao
```

#### Cenário: Exemplo mostra entrada com curinga total
- **QUANDO** o usuário lê a seção de Exemplos
- **ENTÃO** um exemplo com todos os curingas está documentado
