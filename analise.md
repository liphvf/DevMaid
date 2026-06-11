# Code Review: Implementação de API Local com SSE para Query Run

Este documento fornece uma análise técnica da implementação da API de execução de queries, focando em arquitetura, qualidade de código, segurança e performance.

## 1. Arquitetura e Design

### Pontos Fortes
- **Desacoplamento via Observer Pattern**: A introdução de `IProgressObserverService` foi a decisão arquitetural mais acertada. Ela permitiu que o motor de execução no Core fosse agnóstico à interface de saída, suportando simultaneamente a TUI do CLI (via `ConsoleObserverService`) e o stream SSE da API (via `SseEventSinkService`).
- **Extração do Engine para Core**: A lógica foi corretamente movida de `QueryRunCommand` (monolito anterior) para `QueryPlannerService` e `QueryExecutorService`. Isso removeu a duplicação e centralizou as regras de negócio.
- **Gerenciamento de Estado**: O uso de `ExecutionRegistryService` com `ConcurrentDictionary` permite suporte nativo a múltiplas execuções simultâneas (múltiplas abas no Angular), resolvendo um problema potencial de concorrência.
- **Resiliência**: A manutenção do `ResiliencePipeline` (Polly) no `QueryExecutorService` garante que a API herde a robustez de retry já testada no CLI.

### Pontos de Atenção / Melhorias
- **Acoplamento de Modelos**: Os DTOs da API (`QueryApiModels.cs`) estão no projeto `FurLab.Core`. Embora prático, em arquiteturas maiores, isso poderia ser movido para um projeto de contratos separado para evitar que a Core dependa de definições de API.
- **CancellationToken Lifetime**: O `ExecutionContext` mantém o `CancellationTokenSource`. É fundamental garantir que este seja descartado (`Dispose`) ao final de cada execução para evitar vazamentos de memória.

---

## 2. Segurança

### Implementação de Local-Only
- **Defense in Depth**: O sistema implementa segurança em duas camadas:
  1. **Camada de Rede**: Bind do Kestrel via `ListenLocalhost(5000)`.
  2. **Camada de Aplicação**: `LocalhostSecurityMiddleware` que valida explicitamente o `RemoteIpAddress` contra loopback.
- **Risco**: Se o servidor for colocado atrás de um proxy reverso (como Nginx) sem a configuração correta de `X-Forwarded-For`, o middleware pode rejeitar requisições legítimas ou ser enganado por headers falsos. Como a API é estritamente local, esse risco é aceitável.

---

## 3. Performance e Escalabilidade

### SSE e Memória
- ** la-Sinks e Buffering**: O uso de `System.Threading.Channels` no `SseBroadcasterService` é a escolha correta para lidar com a pressão de escrita (backpressure), evitando que a execução de queries trave se o cliente SSE estiver lento.
- **Limitação de Preview**: A decisão de enviar apenas as primeiras 50 linhas (ou 500KB) via SSE evita a saturação da banda de rede e consumo excessivo de RAM no servidor ao lidar com queries que retornam milhões de linhas.

### Paralelismo
- **Controle de Concorrência**: O `QueryExecutorService` respeita o `MaxParallelism` configurado por servidor, evitando que a API derrube o banco de dados com excesso de conexões paralelas.

---

## 4. Qualidade de Código e Manutenibilidade

### Nomenclatura e Estilo
- **Convenções**: A regra de nomenclatura `*Service` foi aplicada rigorosamente em todas as novas classes de infraestrutura e negócio.
- **Clean Code**: O código segue os princípios de responsabilidade única. O `QueryController` é delgado, delegando toda a lógica para os serviços.

### Testabilidade
- **Sustentabilidade**: A separação entre o motor de execução e a interface de observação tornou o sistema altamente testável. 
- **Cobertura**: A implementação foi acompanhada de 39 novos testes unitários cobrindo fluxos críticos e  la-esteste de integração com Testcontainers, mitigando riscos de regressão.

---

## 5. Conclusão e Veredito

**Status: Aprovado para Merge** ✅

A implementação é robusta, segue as diretrizes de arquitetura do projeto e resolve o problema de forma escalável. A extração da lógica do CLI para o Core não apenas habilitou a API, mas melhorou a qualidade geral do código do projeto.

### Sugestões para Futuros Sprints:
1. **TTL para Execuções**: Implementar um timer para limpar automaticamente o `ExecutionRegistryService` de execuções antigas.
2. **Health Check**: Adicionar um endpoint `/health` para o Angular verificar a disponibilidade da API.
3. **SseBroadcaster Refinement**: Implementar suporte a reconexão com "replay" de eventos perdidos usando IDs de eventos no stream.
