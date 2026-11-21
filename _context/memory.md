# Memória do Projeto

> Estado **atual** do projeto. Leia antes de começar qualquer spec e reescreva ao terminar (regras na spec 00, seção 4.8).

**Última atualização:** 2026-11-21

## Estado das specs

| Spec | Título | Status |
| --- | --- | --- |
| 00 | Constituição e convenções | Implementada |
| 01 | Estrutura da solution e empacotamento .NET | Implementada |
| 02 | Contratos de eventos e modelo de dados | Rascunho |
| 03 | Ambiente Docker e LocalStack | Rascunho |
| 04 | Idempotência, resiliência e tratamento de falhas | Rascunho |
| 05 | Observabilidade e proteção de dados | Rascunho |
| 06 | Lambda motor-regras-score | Rascunho |
| 07 | Lambda processador-proposta | Rascunho |
| 08 | Lambda despachante-notificacao | Rascunho |
| 09 | Lambda consulta-proposta | Rascunho |
| 10 | Roteiro de teste ponta a ponta | Rascunho |

## Próximo passo

Implementar a spec 02 — contratos de eventos e modelo de dados.

## Decisões vigentes

- [ADR 0001](../docs/adr/0001-invocacao-sincrona-entre-lambdas.md) — invocação síncrona processador → motor
- [ADR 0002](../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md) — runtime `dotnet10` gerenciado, `x86_64` por padrão
- [ADR 0003](../docs/adr/0003-emulador-local-localstack.md) — LocalStack, condicionada à prova da spec 03
- [ADR 0004](../docs/adr/0004-idempotencia-e-publicacao-no-kafka.md) — idempotência e publicação no Kafka

## Ferramentas validadas

- .NET SDK 10.0.404 e Amazon.Lambda.Tools 7.0.0.
- AWS CLI 2.19.4, `awslocal` 0.23.0, Docker 28.0.4.
- `dotnet build` sem erros nem warnings; `dotnet test` passa com 24 testes.
- LocalStack 2026.10.8 no plano Hobby: as quatro Lambdas `dotnet10` (`x86_64`) foram criadas com os ZIPs de `dist/` e responderam o texto de esqueleto.

## Armadilhas descobertas

- O `awslocal` foi descontinuado em favor do `lstk aws`; ainda funciona.
- A solution usa o formato `.slnx`, com pastas virtuais `src` e `tests`.

## Pendências

- Lambda com `dotnet10` confirmado no Hobby; falta confirmar o ESM com Kafka (spec 03).
- Decidir entre `awslocal` e `lstk` nos scripts (spec 03).
