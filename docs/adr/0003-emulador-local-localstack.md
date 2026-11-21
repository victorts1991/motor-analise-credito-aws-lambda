# ADR 0003 — LocalStack como emulador local da AWS

- **Status:** Aceita, **condicionada à validação da spec 03**
- **Data:** 2026-11-21

## Contexto

O projeto precisa rodar 100% na máquina de quem clonar o repositório, sem conta na AWS e sem pipeline de CI/CD. Os recursos AWS necessários são:

- **Lambda** com runtime `dotnet10`
- **Event Source Mapping (ESM)** consumindo um Kafka *self-managed* (um container Kafka comum, não o MSK)
- **Lambda Function URL**
- **IAM** básico (roles das funções)

Mudanças recentes no cenário:

- Desde **23 de março de 2026**, a imagem do LocalStack exige um **auth token** (`LOCALSTACK_AUTH_TOKEN`) e uma conta criada.
- O plano gratuito (**Hobby**) é restrito a **uso pessoal, educacional e não comercial**.
- Na tabela de planos do LocalStack, o **MSK** não está incluído no Hobby. O suporte ao **ESM com Kafka self-managed** no Hobby **não está documentado** na página de planos e precisa ser confirmado na prática.
- O `awslocal` foi marcado como descontinuado em favor da nova CLI `lstk`.
- Em **novembro de 2026** a AWS lançou o **AWS Emulator**, gratuito e com licença CC0, ainda muito recente.

## Decisão

1. Usar o **LocalStack** (imagem única, com auth token lido do `.env`) para emular Lambda, IAM, ESM e Function URL.
2. Usar **Kafka self-managed** em container (KRaft) em vez de MSK, porque o MSK não está no plano gratuito.
3. A **spec 03** começa com uma prova de conceito mínima: uma Lambda .NET "hello world" acionada por uma mensagem no Kafka via ESM. Essa decisão só é confirmada se essa prova passar.
4. **Plano B**, se o ESM com Kafka não funcionar no plano disponível: um container pequeno (*poller*) que consome os tópicos e invoca as Lambdas no LocalStack com o **mesmo formato de evento** `KafkaEvent`. O código das Lambdas não muda; muda só quem dispara.
5. Confirmar se o uso se enquadra no plano Hobby (não comercial) ou se é necessária uma licença paga.

## Alternativas consideradas

| Alternativa | Por que não agora |
| --- | --- |
| **AWS Emulator** (AWS, CC0) | Gratuito e sem conta, mas lançado em novembro de 2026; ainda não sabemos se executa Lambdas .NET e ESM com Kafka. Vale reavaliar em alguns meses. |
| **AWS SAM CLI (`sam local`)** | Executa Lambdas localmente, mas não emula ESM com Kafka nem a integração entre serviços. |
| **Conta AWS real** | Fidelidade total, mas gera custo e foge do objetivo de rodar localmente. |
| **Rodar as Lambdas como aplicações console** | Perde o objetivo de treinar o modelo de execução do Lambda (ciclo de vida, eventos, empacotamento). |

## Consequências

**Positivas**

- Ambiente próximo do real, com os mesmos comandos da AWS CLI.
- Um único `docker compose up` sobe tudo.

**Negativas**

- Cada pessoa que rodar o projeto precisa criar uma conta no LocalStack e gerar um token.
- Dependência de um fornecedor com termos de licença que mudaram em 2026.
- Risco aberto sobre o ESM com Kafka no plano gratuito, coberto pelo plano B.

## Quando revisitar

- Logo após a prova da spec 03 (confirmar ou ativar o plano B).
- Ao decidir entre `awslocal` e `lstk` nos scripts.
- Se o AWS Emulator amadurecer e passar a cobrir Lambda + ESM com Kafka.