# Spec de Feature: Gerador de Classes a partir de Tabelas

**ID:** 001  
**Slug:** table-parser  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Permitir que desenvolvedores gerem instantaneamente uma classe C# tipada a partir de um schema de tabela PostgreSQL existente, eliminando a tradução manual de definições de colunas em declarações de propriedades.

---

## Histórias de Usuário

**HU-001.1** — Como desenvolvedor, quero me conectar a um banco PostgreSQL e gerar uma classe C# a partir de um schema de tabela, para não precisar escrever manualmente o código boilerplate de modelos.

**HU-001.2** — Como desenvolvedor, quero que colunas anuláveis gerem tipos C# anuláveis (`int?`, `string?`), para que a classe gerada reflita com precisão o schema do banco.

**HU-001.3** — Como desenvolvedor, quero escolher onde o arquivo de saída é salvo, para que eu possa colocá-lo diretamente no diretório do meu projeto.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-001.1 | Dados parâmetros de conexão válidos e uma tabela existente, a ferramenta gera um arquivo `.cs` com uma classe cujo nome corresponde ao nome da tabela (PascalCase). |
| CA-001.2 | Cada coluna se torna uma propriedade pública com o mapeamento correto de tipo C# (ver Regras de Negócio). |
| CA-001.3 | Colunas anuláveis (`IS NULLABLE = YES`) geram tipos de propriedade anuláveis (`int?`, `bool?`, etc.). |
| CA-001.4 | Quando `--output` é omitido, o arquivo é gravado como `./table.class` no diretório de trabalho atual. |
| CA-001.5 | Quando a tabela não existe, a ferramenta sai com código `1` e imprime uma mensagem de erro específica. |
| CA-001.6 | Quando a senha não é fornecida via `--password`, a ferramenta solicita interativamente sem exibir a entrada. |
| CA-001.7 | Tipos PostgreSQL sem suporte geram uma propriedade `object` com um comentário `// [TIPO SEM SUPORTE: <pg_type>]`. |

---

## Interface CLI

```bash
devmaid table-parser [opções]
```

### Opções

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `--database` | `-d` | Sim | — | Nome do banco de dados |
| `--table` | `-t` | Não | — | Nome da tabela a analisar |
| `--user` | `-u` | Não | `postgres` | Usuário do banco |
| `--password` | `-p` | Não | solicitar | Senha do banco |
| `--host` | `-H` | Não | `localhost` | Host do banco |
| `--port` | — | Não | `5432` | Porta do banco |
| `--output` | `-o` | Não | `./table.class` | Caminho do arquivo de saída |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Arquivo de classe gerado com sucesso |
| `1` | Erro de conexão, tabela não encontrada ou erro de gravação |
| `2` | Opção obrigatória ausente (`--database`) |
| `3` | Erro no driver psql/Npgsql |

---

## Mapeamento de Tipos

| Tipo PostgreSQL | Tipo C# |
|----------------|---------|
| `int`, `integer`, `int4` | `int` |
| `bigint`, `int8` | `long` |
| `smallint`, `int2` | `short` |
| `varchar`, `character varying`, `text` | `string` |
| `boolean`, `bool` | `bool` |
| `numeric`, `decimal` | `decimal` |
| `real`, `float4` | `float` |
| `double precision`, `float8` | `double` |
| `timestamp`, `timestamp without time zone` | `DateTime` |
| `timestamp with time zone`, `timestamptz` | `DateTimeOffset` |
| `date` | `DateOnly` |
| `time` | `TimeOnly` |
| `uuid` | `Guid` |
| `bytea` | `byte[]` |
| `json`, `jsonb` | `string` |
| Qualquer outro | `object` + comentário de aviso |

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| Credenciais inválidas | Sair `1`, mensagem: `"Autenticação falhou para o usuário '<usuario>'@'<host>'. Verifique suas credenciais."` |
| Tabela não encontrada | Sair `1`, mensagem: `"Tabela '<tabela>' não encontrada no banco '<banco>'."` |
| Banco não encontrado | Sair `1`, mensagem: `"Banco de dados '<banco>' não existe no host '<host>'."` |
| Timeout de conexão | Sair `1`, mensagem: `"Conexão com '<host>:<porta>' expirou. O PostgreSQL está em execução?"` |
| Caminho de saída inválido | Sair `1`, mensagem: `"Não é possível gravar no caminho '<caminho>'. Verifique as permissões."` |
| Tabela vazia (sem colunas) | Gerar classe vazia com comentário `// Tabela sem colunas.` |

---

## Requisitos Não Funcionais

- A geração deve ser concluída em menos de **2 segundos** para tabelas com até 100 colunas em uma conexão local.
- O arquivo gerado deve ser C# válido e compilável (exceto pelos tipos de fallback `object`).
