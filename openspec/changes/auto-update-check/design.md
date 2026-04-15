## Contexto

O FurLab é distribuído via dois canais: **winget** (`FurLab.CLI`) e **dotnet tool** (`FurLab`). Atualmente não existe nenhum mecanismo de notificação de atualizações. O usuário precisa verificar manualmente.

O projeto já possui `UserConfigService` com persistência em `%LocalAppData%\FurLab\furlab.jsonc`, `IProcessExecutor` para spawn de processos externos, e `HttpClient` disponível via DI. A arquitetura segue o padrão: interface em `FurLab.Core/Interfaces/`, implementação em `FurLab.Core/Services/`, comando em `FurLab.CLI/Commands/`.

## Objetivos / Não-Objetivos

**Objetivos:**
- Verificar automaticamente uma vez por dia se existe nova versão disponível
- Detectar o canal de instalação e persistir para não repetir a detecção
- Notificar o usuário na próxima invocação (não na atual)
- Oferecer `fur update check` e `fur update run` para controle manual
- Falhar silenciosamente em caso de erro de rede ou timeout

**Não-Objetivos:**
- Atualização automática sem confirmação do usuário
- Suporte a múltiplos canais simultâneos (ex: winget + dotnet na mesma máquina)
- Downgrade de versão
- Verificação em intervalos menores que 1 dia
- Rollback após atualização

## Decisões

### D1 — Fonte da versão disponível: NuGet API

**Escolha**: `GET https://api.nuget.org/v3-flatcontainer/furlab/index.json`

A resposta retorna um array de versões ordenadas; a última é a mais recente. Uma única fonte serve para detectar a versão disponível independentemente do canal instalado.

**Alternativas consideradas**:
- `winget show FurLab.CLI` — lento (~1-2s), requer winget instalado, parsing de stdout frágil
- GitHub Releases API — acoplamento ao GitHub, exige user-agent e pode sofrer rate limit sem token
- `dotnet tool list -g` — dá a versão *instalada*, não a *disponível*

---

### D2 — Detecção do canal: `dotnet tool list -g` com fallback winget

**Escolha**: Executar `dotnet tool list -g` e procurar por `furlab` na saída. Se encontrado → canal `dotnet`. Se não encontrado ou `dotnet` não disponível → assume `winget`.

**Rationale**: `dotnet` está sempre presente em máquinas que usam dotnet tool. O winget é o canal majoritário, então assumir winget como fallback é seguro. Não vale spawnar `winget list` apenas para confirmar — custo alto para ganho zero.

**Canal persistido em `furlab.jsonc`**: após detecção, o valor é salvo. Nas verificações seguintes, a detecção é pulada. `fur update check` e `fur update run` sempre re-detectam (ignora cache) para garantir correção em caso de migração de canal.

---

### D3 — Timing da verificação: background task com await no teardown

**Escolha**: A verificação HTTP é iniciada como `Task` no startup (não awaited imediatamente). O comando principal executa normalmente. Ao final do `Main`, a task é awaited com timeout de 3 segundos.

```
Main()
  │
  ├─ [inicia Task de verificação em background]
  ├─ rootCommand.Parse(args).Invoke(...)   ← executa normalmente
  └─ await checkTask (máx 3s restantes)
          │
     se achou versão nova → persiste em furlab.jsonc
```

**Rationale**: O usuário nunca sente latência. O timeout de 3s é generoso o suficiente para redes lentas e curto o suficiente para não travar o terminal.

---

### D4 — Notificação: próxima invocação, não a atual

**Escolha**: Quando a verificação detecta versão nova, persiste `{ latestKnown, notified: false }`. Na próxima invocação, antes de executar o comando, o programa lê esse estado e exibe a notificação.

**Rationale**: Exibir a notificação no meio ou após o output do comando atual seria confuso. Exibir antes do próximo comando é natural — o usuário vê a notificação num momento limpo, antes de qualquer output.

---

### D5 — Recusa: sem nova notificação até versão mais nova

**Escolha**: Se o usuário responde "N", o campo `notified` é marcado `true`. O usuário só verá nova notificação quando `latestKnown` for atualizado para uma versão superior (mínimo no dia seguinte).

**Rationale**: Notificar a cada invocação após recusa seria irritante. A abordagem respeita a decisão do usuário sem silenciar para sempre.

---

### D6 — Após atualização: encerrar, não continuar

**Escolha**: Após executar `winget upgrade` ou `dotnet tool update`, o programa exibe mensagem de sucesso e encerra. O usuário re-roda o comando original.

**Rationale**: O processo em execução é o binário antigo. Continuar executando o comando original com a versão anterior seria tecnicamente correto mas semanticamente confuso para o usuário.

---

### D7 — Armazenamento: campo `updateCheck` no `furlab.jsonc`

**Escolha**: Estender `UserConfig` com campo opcional `UpdateCheckState`:

```jsonc
{
  "updateCheck": {
    "channel": "winget",        // "winget" | "dotnet" | null
    "lastChecked": "2026-04-15", // ISO date string
    "latestKnown": "1.2.0",     // última versão disponível detectada
    "notified": false            // true = usuário já foi notificado desta versão
  }
}
```

**Alternativa considerada**: arquivo separado `furlab.state.json`. Rejeitado por complexidade adicional sem benefício prático — o `furlab.jsonc` já é editado apenas pelo programa na maioria dos campos de controle.

## Riscos / Trade-offs

| Risco | Mitigação |
|---|---|
| NuGet API fora do ar ou lenta | Timeout de 3s, falha silenciosa, `lastChecked` não é atualizado — tenta novamente no dia seguinte |
| Cache de canal incorreto após migração | `fur update check` e `fur update run` sempre re-detectam o canal, ignorando o cache |
| `dotnet tool list -g` retorna formato diferente em versões futuras do .NET | Parsing defensivo: procura linha contendo "furlab" (case-insensitive), não depende de posição de coluna fixa |
| Usuário nunca atualiza mesmo sendo notificado | Fora de escopo — o objetivo é informar, não forçar |
| Versão do assembly não corresponde à versão do NuGet (ex: build local) | Comparação semver: se versão local ≥ NuGet, não notifica. Build local geralmente tem versão de dev maior |
