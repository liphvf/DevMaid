# Spec de Feature: Limpador de Projetos (.NET Clean)

**ID:** 008  
**Slug:** project-cleaner  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Liberar espaço em disco e resolver corrupção de cache de build do .NET excluindo recursivamente todos os diretórios de saída `bin` e `obj` de uma árvore de projetos — uma tarefa comum de desenvolvimento que é tediosa de realizar manualmente em soluções grandes.

---

## Histórias de Usuário

**HU-008.1** — Como desenvolvedor .NET, quero limpar todas as pastas `bin` e `obj` da minha solução com um único comando, para resolver problemas de cache de build sem navegar por cada diretório de projeto.

**HU-008.2** — Como desenvolvedor, quero direcionar um diretório específico para limpeza, para limpar um projeto sem alterar meu diretório de trabalho atual.

**HU-008.3** — Como desenvolvedor, quero ver quanto espaço em disco foi liberado, para saber que a operação de limpeza foi eficaz.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-008.1 | `devmaid clean` exclui recursivamente todos os diretórios `bin` e `obj` encontrados no diretório de trabalho atual. |
| CA-008.2 | `devmaid clean <caminho>` exclui recursivamente os diretórios `bin` e `obj` em `<caminho>`. |
| CA-008.3 | Após a limpeza, a ferramenta imprime o número de diretórios excluídos e o espaço total em disco liberado (em MB). |
| CA-008.4 | Se nenhum diretório `bin` ou `obj` for encontrado, a ferramenta sai `0` com mensagem: `"Nada para limpar."` |
| CA-008.5 | Se um diretório não puder ser excluído (ex.: arquivos bloqueados), a ferramenta registra um aviso para aquele caminho e continua com os demais. |
| CA-008.6 | O caminho fornecido deve existir; se não existir, sair `1` com uma mensagem de erro descritiva. |

---

## Interface CLI

```bash
devmaid clean [<caminho>]
```

### Argumentos

| Argumento | Obrigatório | Padrão | Descrição |
|-----------|-------------|--------|-----------|
| `<caminho>` | Não | `./` (diretório atual) | Diretório raiz para limpar |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Limpeza concluída (mesmo que nada tenha sido encontrado) |
| `1` | Caminho especificado não existe |
| `1` | Falha parcial (alguns diretórios não puderam ser excluídos) |

---

## Exemplo de Saída

```
Verificando: C:\Projects\MySolution
Encontrados 12 diretórios para limpar (bin: 8, obj: 4)

  Excluído: C:\Projects\MySolution\ProjectA\bin
  Excluído: C:\Projects\MySolution\ProjectA\obj
  Excluído: C:\Projects\MySolution\ProjectB\bin
  ...
  AVISO: Não foi possível excluir C:\Projects\MySolution\ProjectC\bin (arquivos em uso)

Limpeza concluída.
  Diretórios excluídos: 11 / 12
  Espaço em disco liberado: 340,2 MB
```

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| Caminho não existe | Sair `1`, mensagem: `"Caminho '<caminho>' não existe."` |
| Permissões insuficientes | Registrar aviso por diretório, continuar com os demais |
| Arquivos bloqueados (ex.: processo em execução) | Registrar aviso por diretório, continuar com os demais |
| Nada para limpar | Sair `0`, mensagem: `"Nada para limpar em '<caminho>'."` |

---

## Requisitos Não Funcionais

- Deve ser concluído em menos de **10 segundos** para soluções com até 50 projetos.
- Não deve seguir links simbólicos para evitar exclusões não intencionais fora da árvore alvo.
