# Spec de Feature: Gerenciador de Recursos do Windows

**ID:** 009  
**Slug:** windows-features-manager  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Permitir que desenvolvedores exportem seus Recursos Opcionais do Windows atualmente ativados para um arquivo JSON de backup, e posteriormente importem/habilitem esses recursos em uma instalação nova do Windows ou em uma nova máquina — eliminando a necessidade de reativar recursos manualmente pela interface do Windows.

---

## Histórias de Usuário

**HU-009.1** — Como desenvolvedor configurando uma nova máquina, quero restaurar todos os meus Recursos Opcionais do Windows previamente ativados a partir de um arquivo de backup, para que meu ambiente seja consistente com minha configuração anterior.

**HU-009.2** — Como desenvolvedor se preparando para reinstalar o Windows, quero exportar todos os recursos opcionais habilitados para um arquivo, para poder restaurá-los depois sem precisar lembrar quais estavam ativos.

**HU-009.3** — Como desenvolvedor, quero listar todos os recursos opcionais atualmente ativados, para auditar meu ambiente antes ou depois de fazer alterações.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-009.1 | `devmaid windowsfeatures list --enabled-only` imprime todos os Recursos Opcionais do Windows atualmente habilitados, um por linha. |
| CA-009.2 | `devmaid windowsfeatures list` (sem `--enabled-only`) imprime todos os recursos com seu estado (Habilitado/Desabilitado). |
| CA-009.3 | `devmaid windowsfeatures export <caminho>` grava todos os recursos habilitados em `<caminho>` como arquivo JSON. |
| CA-009.4 | `devmaid windowsfeatures import <caminho>` lê o arquivo JSON e habilita todos os recursos listados usando `dism.exe`. |
| CA-009.5 | Se o `dism.exe` não for encontrado, tanto `export` quanto `import` saem `3` com uma mensagem de erro. |
| CA-009.6 | Durante a importação, cada recurso é habilitado de forma independente; a falha em um recurso não aborta os recursos restantes. |
| CA-009.7 | O comando `import` imprime um resultado por recurso (sucesso/falha) e um resumo final. |

---

## Interface CLI

```bash
devmaid windowsfeatures list [--enabled-only]
devmaid windowsfeatures export <caminho>
devmaid windowsfeatures import <caminho>
```

### Argumentos e Opções

#### list

| Opção | Obrigatória | Padrão | Descrição |
|-------|-------------|--------|-----------|
| `--enabled-only` | Não | `false` | Mostrar apenas recursos habilitados |

#### export

| Argumento | Obrigatório | Padrão | Descrição |
|-----------|-------------|--------|-----------|
| `<caminho>` | Sim | — | Caminho do arquivo JSON de saída |

#### import

| Argumento | Obrigatório | Padrão | Descrição |
|-----------|-------------|--------|-----------|
| `<caminho>` | Sim | — | Arquivo JSON a importar |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Operação concluída com sucesso |
| `1` | Erro de E/S de arquivo ou falha parcial na importação |
| `2` | Argumento obrigatório ausente |
| `3` | `dism.exe` não encontrado |

---

## Formato do Arquivo de Exportação

```json
{
  "ExportDate": "2026-04-01T10:30:00Z",
  "Features": [
    { "FeatureName": "Microsoft-Windows-Subsystem-Linux", "State": "Enabled" },
    { "FeatureName": "VirtualMachinePlatform", "State": "Enabled" },
    { "FeatureName": "Containers", "State": "Enabled" }
  ]
}
```

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| `dism.exe` não encontrado | Sair `3`, mensagem: `"dism.exe não encontrado. Este comando requer Windows com DISM instalado."` |
| Caminho de exportação sem permissão de escrita | Sair `1`, mensagem: `"Não é possível gravar em '<caminho>'. Verifique as permissões."` |
| Arquivo de importação não encontrado | Sair `1`, mensagem: `"Arquivo de importação '<caminho>' não encontrado."` |
| Recurso não disponível nesta edição do Windows | Registrar aviso por recurso, continuar com o próximo |
| Recurso requer reinicialização | Registrar informação: `"Alguns recursos requerem reinicialização do sistema para entrar em vigor."` |
| Nenhum recurso habilitado | Exportação grava JSON com array `Features` vazio |

---

## Requisitos Não Funcionais

- Deve executar apenas no Windows. Se invocado em outro sistema operacional, sair `1` com mensagem: `"Este comando requer Windows."`.
- A importação deve ser idempotente — habilitar um recurso já habilitado não deve produzir erro.
- As operações de exportação e listagem devem ser concluídas em menos de **5 segundos**.
