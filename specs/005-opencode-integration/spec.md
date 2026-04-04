# Spec de Feature: Integração com OpenCode

**ID:** 005  
**Slug:** opencode-integration  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Automatizar a instalação e configuração da ferramenta CLI OpenCode, permitindo que desenvolvedores configurem seu ambiente OpenCode com um único comando DevMaid, em vez de navegar por documentação externa.

---

## Histórias de Usuário

**HU-005.1** — Como desenvolvedor, quero instalar o OpenCode pelo DevMaid, para não precisar procurar e seguir instruções externas de instalação.

**HU-005.2** — Como desenvolvedor, quero verificar se o OpenCode está instalado e qual versão está ativa, para confirmar que meu ambiente está pronto.

**HU-005.3** — Como desenvolvedor, quero configurar o OpenCode pelo DevMaid, para aplicar configurações padrão para meu fluxo de trabalho sem editar arquivos de configuração manualmente.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-005.1 | `devmaid opencode install` instala o OpenCode via gerenciador de pacotes disponível e sai `0` em caso de sucesso. |
| CA-005.2 | Se o OpenCode já estiver instalado, `install` pula a instalação, imprime a versão instalada e sai `0`. |
| CA-005.3 | `devmaid opencode status` imprime a versão instalada ou `"OpenCode não está instalado."` |
| CA-005.4 | `devmaid opencode config` aplica a configuração padrão recomendada pelo DevMaid para o OpenCode. |
| CA-005.5 | Se o OpenCode não for encontrado no PATH após a instalação, a ferramenta imprime uma sugestão de atualização do PATH. |

---

## Interface CLI

```bash
devmaid opencode install
devmaid opencode status
devmaid opencode config
```

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Operação concluída com sucesso |
| `1` | Falha na instalação ou configuração |
| `2` | Subcomando inválido |
| `3` | Gerenciador de pacotes necessário não encontrado |

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| Falha na instalação | Sair `1`, imprimir erro do gerenciador de pacotes |
| Já instalado | Sair `0`, imprimir informações de versão |
| Não encontrado no PATH após instalação | Sair `0` com aviso: `"OpenCode instalado mas não encontrado no PATH. Pode ser necessário reiniciar o terminal ou atualizar o PATH."` |
| Arquivo de configuração ausente | Criar arquivo de configuração com valores padrão |

---

## Requisitos Não Funcionais

- A instalação deve ser idempotente — executar `install` várias vezes não deve criar efeitos colaterais.
- `status` deve ser concluído em menos de **1 segundo**.
