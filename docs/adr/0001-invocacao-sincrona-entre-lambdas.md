# ADR 0001 — Invocação síncrona entre Lambdas (processador → motor de score)

- **Status:** Aceita
- **Data:** 2026-11-21

## Contexto

O cálculo de score é uma regra de negócio determinística: a mesma entrada sempre produz a mesma saída e não precisa de banco nem de rede. Queremos que essa regra fique isolada, testável sem nenhum mock de infraestrutura e com ciclo de deploy independente do restante da esteira.

A Lambda `processador-proposta` precisa do resultado do score **antes** de gravar a decisão e publicar o evento `credito.propostas.avaliadas`. Ou seja, o fluxo depende da resposta e não pode seguir sem ela.

## Decisão

O `processador-proposta` invoca a Lambda `motor-regras-score` de forma **síncrona** (`InvocationType = RequestResponse`) via `AWSSDK.Lambda`.

Regras que acompanham a decisão:

1. **Contrato explícito.** Entrada `AvaliacaoScoreRequest` e saída `AvaliacaoScoreResponse` ficam no projeto `MotorCredito.Dominio`, compartilhado pelas duas Lambdas.
2. **Abstração no chamador.** O processador depende de uma interface (`IMotorScoreClient`), não do SDK diretamente. A implementação padrão usa `AmazonLambdaClient`; trocar por uma chamada em memória exige só outra implementação da interface.
3. **Tratamento de erro.** Uma exceção dentro do motor não vira exceção HTTP no chamador: o `Invoke` retorna sucesso com o campo `FunctionError` preenchido. O cliente deve verificar `FunctionError` e transformar em exceção de domínio.
4. **Timeouts encadeados.** O timeout do processador deve ser maior que o do motor somado à latência da chamada, para o processador nunca morrer esperando.
5. **Cliente reaproveitado.** O `AmazonLambdaClient` é criado uma única vez, fora do handler.

## Alternativas consideradas

| Alternativa | Por que não agora |
| --- | --- |
| **Motor como biblioteca dentro do processador** (chamada em memória) | É a opção mais barata e simples, e provavelmente a recomendada em produção. Fica descartada **só para a POC**, porque o objetivo é treinar a comunicação entre Lambdas. A interface `IMotorScoreClient` mantém essa troca a uma implementação de distância. |
| **AWS Step Functions** orquestrando as etapas | Resolve orquestração e retries de forma declarativa, mas adiciona um serviço a mais para emular e estudar. Fica para uma evolução futura. |
| **Comunicação assíncrona via Kafka** (tópico de pedido de score e tópico de resposta) | Desacopla totalmente, mas exige correlação de mensagens e torna o fluxo bem mais complexo para um cálculo que leva milissegundos. |

## Consequências

**Positivas**

- O domínio fica isolado de verdade: a Lambda do motor não referencia o projeto de infraestrutura.
- O motor pode ser testado só com dados de entrada e saída.
- O time pratica invocação entre Lambdas, contratos compartilhados e tratamento de `FunctionError`.

**Negativas (e conhecidas)**

- **Custo duplicado:** enquanto o motor executa, o processador fica parado aguardando e também é cobrado. A AWS documenta esse padrão como antipadrão para produção.
- **Cold start em cadeia:** se as duas Lambdas estiverem frias, as latências se somam.
- **Acoplamento temporal:** se o motor estiver indisponível, o processador falha e o lote do Kafka é reentregue. A idempotência (ADR 0004) garante que isso não gere duplicidade.

## Quando revisitar

Antes de levar o padrão para produção, ou se as medições de latência e custo mostrarem que a chamada síncrona é um gargalo. Nesse caso, a primeira opção é a alternativa da biblioteca em memória.