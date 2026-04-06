## Por que

Configurar um container PostgreSQL local para desenvolvimento requer memorizar e digitar um comando `docker run` longo com múltiplos parâmetros. O comando `devmaid docker postgres` elimina essa fricção, permitindo subir um PostgreSQL pronto para uso com uma única instrução.

## O que Muda

- Novo comando `devmaid docker` como grupo pai para utilitários Docker.
- Novo subcomando `devmaid docker postgres` que executa o container PostgreSQL pré-configurado:
  - Imagem `postgres:alpine` (leve)
  - Nome do container: `postgres-ptbr`
  - Política de reinício: `always`
  - Porta: `5432:5432`
  - Senha padrão: `dev`
  - Locale pt-BR (`LANG=pt_BR.UTF-8`, `LC_ALL=pt_BR.UTF-8`)
  - Volume persistente: `postgres-data:/var/lib/postgresql/data`
  - Log completo de queries (`log_statement=all`, `log_min_duration_statement=0`)
- O comando verifica se o Docker está disponível antes de executar.
- Exibe o ID do container e instruções de conexão ao concluir.

## Capacidades

### Novas Capacidades

- `docker-postgres`: Subcomando que provisiona um container PostgreSQL de desenvolvimento com locale pt-BR, logging completo e volume persistente via uma única chamada ao CLI.

### Capacidades Modificadas

_(nenhuma)_

## Impacto

- **Código novo**: `DevMaid.CLI/Commands/DockerCommand.cs` com subcomando `postgres`.
- **Registro**: `Program.cs` precisa registrar o novo `DockerCommand`.
- **Dependência externa**: requer Docker instalado e em execução na máquina do usuário.
- **Sem novas dependências de pacote**: usa `System.Diagnostics.Process` (já presente no projeto).
