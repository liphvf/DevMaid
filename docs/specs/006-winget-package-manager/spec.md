# Spec de Feature: Gerenciador de Pacotes Winget

**ID:** 006  
**Slug:** winget-package-manager  
**Status:** Implementado  
**Versão:** 1.0  

---

## Propósito

Permitir que desenvolvedores façam backup e restauração do seu ambiente de aplicativos Windows (via winget) ao migrar para uma nova máquina, reinstalar o Windows ou replicar uma configuração de desenvolvimento entre máquinas.

---

## Histórias de Usuário

**HU-006.1** — Como desenvolvedor migrando para uma nova máquina, quero exportar todos os meus pacotes instalados via winget para um arquivo JSON, para que eu possa reproduzir meu ambiente de desenvolvimento na nova máquina.

**HU-006.2** — Como desenvolvedor em uma instalação nova do Windows, quero restaurar todos os pacotes a partir de um arquivo de backup, para recuperar automaticamente meu conjunto completo de ferramentas.

**HU-006.3** — Como desenvolvedor, quero que o arquivo de backup inclua informações de versão, para rastrear o que estava instalado em um determinado momento.

---

## Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-006.1 | `devmaid winget backup` cria `backup-winget.json` no diretório atual (ou no diretório `--output`) contendo a lista de pacotes instalados com seus IDs e versões. |
| CA-006.2 | O JSON de backup inclui um timestamp `CreationDate` (ISO 8601). |
| CA-006.3 | `devmaid winget restore --input <arquivo>` executa `winget import` a partir do arquivo JSON especificado. |
| CA-006.4 | Se o arquivo de backup já existir durante o `backup`, a ferramenta solicita confirmação de sobrescrita antes de prosseguir. |
| CA-006.5 | Se um pacote falhar na restauração, a ferramenta registra a falha mas continua restaurando os pacotes restantes. |
| CA-006.6 | Se o winget não estiver instalado, ambos os comandos saem `3` com instruções de instalação. |

---

## Interface CLI

```bash
devmaid winget backup [--output <diretório>]
devmaid winget restore --input <arquivo>
```

### Opções

#### Backup

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `--output` | `-o` | Não | `./` | Diretório de saída para o arquivo de backup |

#### Restauração

| Opção | Curta | Obrigatória | Padrão | Descrição |
|-------|-------|-------------|--------|-----------|
| `--input` | `-i` | Sim | — | Caminho para o arquivo JSON de backup |

### Códigos de Saída

| Código | Cenário |
|--------|---------|
| `0` | Operação concluída com sucesso |
| `1` | Erro de E/S de arquivo ou falha parcial na restauração |
| `2` | Opção obrigatória ausente |
| `3` | winget não encontrado |

---

## Formato do Arquivo de Backup

```json
{
  "CreationDate": "2026-04-01T10:30:00Z",
  "Packages": [
    { "Id": "Git.Git", "Version": "2.44.0" },
    { "Id": "Microsoft.VisualStudioCode", "Version": "1.88.0" }
  ]
}
```

---

## Cenários de Erro

| Cenário | Comportamento Esperado |
|---------|----------------------|
| winget não instalado | Sair `3`, mensagem: `"winget não está instalado. Instale o App Installer pela Microsoft Store."` |
| Nenhum pacote instalado | Criar backup com array `Packages` vazio e `CreationDate` |
| Arquivo de backup já existe | Solicitar: `"O arquivo '<caminho>' já existe. Sobrescrever? [s/N]"`. Abortar em `N`. |
| Arquivo de restauração não encontrado | Sair `1`, mensagem: `"Arquivo de backup '<caminho>' não encontrado."` |
| Pacote indisponível durante restauração | Registrar aviso por pacote, continuar com o próximo, reportar resumo ao final |
| Rede indisponível durante restauração | Sair `1`, mensagem: `"Rede indisponível. Conecte-se à internet e tente novamente."` |

---

## Requisitos Não Funcionais

- O backup deve ser concluído em menos de **30 segundos** para ambientes com até 200 pacotes.
- As operações de restauração não devem exigir privilégios de administrador, a menos que pacotes individuais requeiram elevação (nesse caso, o winget trata o prompt do UAC).
