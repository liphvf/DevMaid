## Contexto

O comando `FurLab database pgpass` permite que desenvolvedores gerenciem arquivos de senha do PostgreSQL para desenvolvimento local. O modelo subjacente (`PgPassEntry.cs`) já documenta suporte a curinga (`*`) em todos os campos, mas a camada de validação em `SecurityUtils.cs` rejeita `*` para host, porta e usuário.

Esta é uma mudança localizada em três métodos de validação, sem preocupações transversais.

## Objetivos / Fora do Escopo

**Objetivos:**
- Habilitar `*` como entrada válida em `IsValidHost()`, `IsValidPort()` e `IsValidUsername()`
- Alinhar a validação à semântica nativa de curingas do pgpass.conf do PostgreSQL
- Atualizar a documentação do contrato para refletir o suporte a curingas

**Fora do Escopo:**
- Senha nunca aceitará `*` (deve ser sempre uma senha real)
- Nenhuma alteração no modelo `PgPassEntry` (já está correto)
- Nenhuma alteração na lógica de serialização/escape do `PgPassService` (já está correta)

## Decisões

| Decisão | Escolha | Justificativa |
|---------|---------|---------------|
| Como tratar `*` na validação? | Retorno antecipado `true` antes do pattern matching | Separação clara: curinga é conceitualmente diferente de "hostname/IP válido", mas válido no contexto pgpass |
| Onde validar? | Manter validação em `SecurityUtils.cs` | Responsabilidade única; métodos de validação são reutilizados em outros contextos |
| Atualização de documentação? | Atualizar apenas o arquivo de contrato | Nenhuma nova spec necessária; é apenas uma flexibilização do comportamento existente |

**Alternativa considerada:** Remover totalmente a validação para os campos pgpass.
- Rejeitada: A validação continua útil para entradas não-curinga (captura hostnames malformados, portas fora do intervalo, etc.)

## Riscos / Trade-offs

| Risco | Mitigação |
|-------|-----------|
| Usuários podem usar `*` em contextos de produção inadvertidamente | Documentar claramente que `*` é curinga; usuário opta explicitamente |
| Nenhum risco adicional | A mudança é puramente aditiva — apenas expande as entradas aceitas |
