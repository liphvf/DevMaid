# Spec de Feature: Utilitários de Arquivo — Combinar

**ID:** 003  
**Slug:** file-combine  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Permitir que desenvolvedores mesclem múltiplos arquivos de texto correspondentes a um padrão glob em um único arquivo de saída, preservando a ordem do conteúdo e a codificação. Usado principalmente para consolidar scripts SQL, arquivos de log e outros artefatos em texto simples.

---

## Histórias de Usuário

**HU-003.1** — Como desenvolvedor, quero combinar todos os arquivos `.sql` de um diretório em um único arquivo, para executar um pacote de migração como um único script.

**HU-003.2** — Como desenvolvedor, quero que o arquivo combinado respeite a ordem alfabética, para que a sequência de arquivos seja previsível e determinística.

**HU-003.3** — Como desenvolvedor, quero que um nome de arquivo de saída padrão seja criado quando não especifico `--output`, para não precisar pensar em nomenclatura em operações rápidas.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-003.1 | Dado um padrão glob válido correspondendo a pelo menos um arquivo, a ferramenta cria um arquivo de saída com o conteúdo concatenado de todos os arquivos encontrados. |
| CA-003.2 | Os arquivos são combinados em **ordem alfabética sem distinção de maiúsculas/minúsculas** pelo nome do arquivo. |
| CA-003.3 | Quando `--output` não é especificado, o arquivo de saída é nomeado `CombineFiles.<ext>` (usando a extensão dos arquivos encontrados) no mesmo diretório dos arquivos. |
| CA-003.4 | A saída é gravada em codificação **UTF-8**, independentemente da codificação dos arquivos de entrada. |
| CA-003.5 | Se o arquivo de saída já existir, ele é **sobrescrito** sem confirmação. |
| CA-003.6 | Quando nenhum arquivo corresponde ao padrão, a ferramenta sai com código `1` e imprime `"Nenhum arquivo correspondendo a '<padrão>' foi encontrado."` |
| CA-003.7 | Quando o padrão está vazio ou ausente, a ferramenta sai com código `2` e imprime uma dica de uso. |

---

## Interface CLI

```bash
devmaid file combine --input <padrão> [--output <arquivo>]
```

### Opções

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `--input` | `-i` | Sim | — | Padrão glob para arquivos de entrada (ex.: `C:\temp\*.sql`) |
| `--output` | `-o` | Não | `CombineFiles.<ext>` | Caminho do arquivo de saída |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Arquivos combinados com sucesso |
| `1` | Nenhum arquivo correspondeu ao padrão |
| `2` | Padrão inválido ou vazio |

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| `--input` vazio | Sair `2`, mensagem: `"O padrão de entrada é obrigatório."` |
| Sintaxe de padrão inválida | Sair `2`, mensagem: `"O padrão de entrada '<padrão>' é inválido."` |
| Nenhum arquivo corresponde | Sair `1`, mensagem: `"Nenhum arquivo correspondendo a '<padrão>' foi encontrado."` |
| Caminho de saída inacessível | Sair `1`, mensagem: `"Não é possível gravar em '<caminho>'. Verifique as permissões."` |

---

## Requisitos Não Funcionais

- Deve processar arquivos de qualquer tamanho sem carregar todo o conteúdo na memória de uma vez (gravação em streaming).
- Deve ser concluído em menos de **5 segundos** para até 500 arquivos com média de 100 KB cada.
