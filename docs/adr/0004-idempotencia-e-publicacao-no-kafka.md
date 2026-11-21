# ADR 0004 — Idempotência e publicação no Kafka (escrita dupla)

- **Status:** Aceita
- **Data:** 2026-11-21

## Contexto

O Kafka, via Event Source Mapping, entrega mensagens **pelo menos uma vez** (*at-least-once*): se a Lambda falhar ou estourar o timeout, o lote inteiro é entregue de novo.

A Lambda `processador-proposta` faz duas escritas em sistemas diferentes:

1. Grava a decisão no **MongoDB** (coleção `propostas`).
2. Publica a decisão no **Kafka** (tópico `credito.propostas.avaliadas`).

Não existe transação que englobe MongoDB e Kafka juntos. Se a Lambda gravar no Mongo e cair antes de publicar, uma reentrega que simplesmente "pula propostas já processadas" faria o evento **sumir**.

## Decisão

### 1. Chave e índices

- A chave de idempotência é o **`idProposta`**.
- A coleção `propostas` tem **índice único** em `idProposta`.
- A coleção `auditoria_propostas` tem **índice único composto** em `idProposta` + `evento`.
- A mensagem Kafka é publicada com **chave = `idProposta`**, para que todos os eventos de uma proposta caiam na mesma partição e sejam processados em ordem.

### 2. Estados da proposta

```text
EM_PROCESSAMENTO → APROVADA | REPROVADA
                         └─ publicadoEm preenchido quando o Kafka confirmar a publicação
```

O documento nasce como `EM_PROCESSAMENTO`, com `UpdateOne` + `$setOnInsert` + `IsUpsert = true`. Se ele já existir, o upsert não altera nada, e o processador lê o estado atual para decidir o que fazer.

### 3. Regras de reprocessamento

| Estado encontrado ao receber a mensagem | Ação |
| --- | --- |
| Não existe | Cria como `EM_PROCESSAMENTO`, chama o motor, grava a decisão, publica e preenche `publicadoEm`. |
| `EM_PROCESSAMENTO` | Execução anterior caiu antes de gravar a decisão. Chama o motor de novo (ele é determinístico, então o resultado é o mesmo) e segue o fluxo. |
| `APROVADA` ou `REPROVADA` **sem** `publicadoEm` | Execução anterior caiu entre gravar e publicar. **Não recalcula**; só publica a decisão gravada e preenche `publicadoEm`. |
| `APROVADA` ou `REPROVADA` **com** `publicadoEm` | Já foi tudo feito. Ignora a mensagem e registra em log como duplicata. |

### 4. Publicação confirmada

O `publicadoEm` só é preenchido depois que o producer confirmar a entrega (`ProduceAsync` concluído com sucesso). O producer faz `Flush` antes do handler retornar.

### 5. Consequência para o consumidor

Como a publicação pode acontecer mais de uma vez (falha depois de publicar e antes de gravar `publicadoEm`), o tópico `credito.propostas.avaliadas` também é *at-least-once*. Por isso a Lambda `despachante-notificacao` **também é idempotente**: o índice único de `auditoria_propostas` impede o registro duplicado, e o erro de chave duplicada é tratado como sucesso.

## Alternativas consideradas

| Alternativa | Por que não agora |
| --- | --- |
| **Transactional Outbox** (gravar o evento numa coleção `outbox` na mesma transação da decisão e ter um *relay* que publica) | É o padrão mais robusto, mas exige MongoDB em *replica set* (para transações) e mais um componente para publicar. Fica registrado como evolução natural. |
| **Transações do Kafka / exactly-once** | Garantem atomicidade só dentro do Kafka; não cobrem a gravação no MongoDB. |
| **Ignorar qualquer proposta já existente** | Simples, mas perde eventos no cenário de falha entre gravar e publicar. |
| **Idempotência via Powertools for AWS Lambda** | Funciona bem, mas o armazenamento padrão é o DynamoDB, que adicionaria mais um serviço ao projeto. |

## Consequências

**Positivas**

- Nenhuma proposta é recalculada nem auditada em duplicidade.
- Nenhum evento de decisão se perde em falhas entre as duas escritas.
- Os cenários de falha viram casos de teste claros (ver specs 04 e 07).

**Negativas**

- Mais lógica no processador (a tabela de reprocessamento).
- O tópico de saída pode ter eventos duplicados, o que empurra a responsabilidade de idempotência também para o consumidor.

## Quando revisitar

Se o projeto evoluir para produção ou para múltiplos consumidores do tópico `credito.propostas.avaliadas`. Nesse caso, avaliar o Transactional Outbox.