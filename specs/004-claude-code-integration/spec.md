# Spec de Feature: Integração com Claude Code

**ID:** 004  
**Slug:** claude-code-integration  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Automatizar a instalação e configuração da ferramenta CLI Claude Code da Anthropic no Windows, incluindo integração com banco de dados via MCP e configurações de ambiente específicas do Windows, reduzindo uma configuração manual de múltiplas etapas a um único comando.

---

## Histórias de Usuário

**HU-004.1** — Como desenvolvedor configurando uma nova máquina Windows, quero instalar o Claude Code com um único comando, para não precisar pesquisar o nome do pacote winget e as flags corretas.

**HU-004.2** — Como desenvolvedor, quero configurar a ferramenta MCP de banco de dados do Claude Code automaticamente, para que o Claude possa consultar meus bancos PostgreSQL locais durante as sessões de codificação.

**HU-004.3** — Como desenvolvedor, quero configurar as definições de ambiente Windows do Claude (shell, permissões), para que o Claude funcione corretamente no Windows sem edição manual de JSON.

**HU-004.4** — Como desenvolvedor, quero verificar se o Claude Code está instalado e qual é sua versão atual, para saber se meu ambiente está pronto.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-004.1 | `devmaid claude install` invoca `winget install` para o pacote Claude Code e sai `0` em caso de sucesso. |
| CA-004.2 | Se o Claude Code já estiver instalado, `install` pula a instalação, imprime uma mensagem de status e sai `0`. |
| CA-004.3 | `devmaid claude status` imprime a versão instalada (se encontrada) ou uma mensagem "não instalado". |
| CA-004.4 | `devmaid claude settings mcp-database` escreve o bloco de configuração MCP no arquivo de configurações do usuário do Claude. |
| CA-004.5 | `devmaid claude settings win-env` escreve as configurações de shell e permissões do Windows no arquivo de configurações do Claude. |
| CA-004.6 | Se o winget não estiver disponível, `install` sai `3` com instruções para instalar o winget. |
| CA-004.7 | Se o arquivo de configurações do Claude não existir, os subcomandos `settings` o criam. |

---

## Interface CLI

```bash
devmaid claude install
devmaid claude status
devmaid claude settings mcp-database
devmaid claude settings win-env
```

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Operação concluída com sucesso |
| `1` | Falha na instalação ou na gravação das configurações |
| `2` | Subcomando inválido |
| `3` | winget não encontrado |

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| winget não instalado | Sair `3`, imprimir: `"winget não está instalado. Instale o App Installer pela Microsoft Store."` |
| Falha na instalação (erro do winget) | Sair `1`, imprimir a saída de erro do winget |
| Diretório do arquivo de configurações ausente | Criar o diretório e então gravar o arquivo |
| Já instalado | Sair `0`, imprimir informações da versão atual |

---

## Requisitos Não Funcionais

- Deve executar apenas no Windows. Se invocado em outro sistema operacional, sair `1` com mensagem: `"Este comando requer Windows."`.
- Modificações de configurações devem ser idempotentes — executar o mesmo comando `settings` duas vezes não deve duplicar entradas.
