# Memória do Projeto

> Estado **atual** do projeto. Leia antes de começar qualquer spec e reescreva ao terminar (regras na spec 00, seção 4.8).

**Última atualização:** 2026-11-21

## Estado das specs

| Spec | Título | Status |
| --- | --- | --- |
| 00 | Constituição e convenções | Aprovada |
| 01 | Estrutura da solution e empacotamento .NET | Aprovada |
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

Implementar a spec 01 com o prompt `/implementar-especificacao`, na branch `spec/01-estrutura-solution`.

## Decisões vigentes

- [ADR 0001](../docs/adr/0001-invocacao-sincrona-entre-lambdas.md) — invocação síncrona processador → motor
- [ADR 0002](../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md) — runtime `dotnet10` gerenciado, `x86_64` por padrão
- [ADR 0003](../docs/adr/0003-emulador-local-localstack.md) — LocalStack, condicionada à prova da spec 03
- [ADR 0004](../docs/adr/0004-idempotencia-e-publicacao-no-kafka.md) — idempotência e publicação no Kafka

## Ferramentas validadas

- .NET SDK 10.0.404 (o projeto fixa o SDK 10 via `global.json`, mesmo com outros SDKs instalados).
- Amazon.Lambda.Tools 7.0.0, AWS CLI 2.19.4, awslocal 0.23.0, Docker 28.0.4.

## Armadilhas descobertas

- O `awslocal` foi descontinuado em favor do `lstk aws`; ainda funciona.

## Pendências

- Confirmar se o plano Hobby (não comercial) do LocalStack atende ao uso da POC.
- Validar o ESM com Kafka no LocalStack (spec 03).
- Decidir entre `awslocal` e `lstk` nos scripts (spec 03).