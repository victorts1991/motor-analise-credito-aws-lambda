# ⚡ Credit Proposal Serverless Stream & Automated Risk Engine

[![Platform](https://img.shields.io/badge/Platform-Node.js%2020.x%20%7C%20TypeScript-blue)](https://nodejs.org/)
[![Runtime](https://img.shields.io/badge/Runtime-AWS%20Lambda-orange)](https://aws.amazon.com/lambda/)
[![Broker](https://img.shields.io/badge/Broker-Apache%20Kafka%20(ESM)-red)](https://kafka.apache.org/)
[![Database](https://img.shields.io/badge/Database-MongoDB-green)](https://www.mongodb.com/)
[![Local](https://img.shields.io/badge/LocalStack-100%25%20Local-black)](https://localstack.cloud/)

---

## 1. Mini Resumo

Solução de referência de arquitetura serverless orientada a eventos (**Event-Driven Architecture - EDA**) executada **100% em ambiente local** (custo zero). O projeto simula uma esteira transacional de crédito composta por funções **AWS Lambda (Node.js 20 / TypeScript)** orquestradas sobre **LocalStack**, consumindo e produzindo eventos no **Apache Kafka** via **Event Source Mapping (ESM)**, persistência no **MongoDB**, interface visual para mensageria e suíte de testes unitários com **Vitest**.

---

## 2. Objetivo e Qual Problema Resolve

Em esteiras transacionais de crédito com alta concorrência de requisições, as equipes de engenharia enfrentam desafios operacionais clássicos:

1. **Gargalo de I/O em Motores de Risco:** Motores de decisão financeira são computacionalmente determinísticos, mas frequentemente acoplados diretamente ao banco de dados e à mensageria, criando contenção de pool de conexões e atrasando a esteira.
2. **Esgotamento de Recursos em Serverless (Connection Storm):** Quando o tráfego do Kafka aumenta e a AWS instancia dezenas de instâncias concorrentes de Lambdas, a abertura desordenada de conexões com o MongoDB colapsa a camada de dados.
3. **Semântica de Entrega *At-Least-Once* do Kafka:** Falhas transitórias geram reentrega de eventos idênticos. Sem mecanismos estritos de idempotência na Lambda, a mesma proposta de crédito seria recalculada e reauditada, gerando duplicidade.

### A Solução Proposta:
- **Segregação de Responsabilidade (I/O vs. Domínio Puro):** A função de entrada orquestra I/O (Kafka + MongoDB) e invoca de forma síncrona ponto a ponto (`RequestResponse`) o motor de risco puro, que roda isolado sem dependência de rede externa.
- **Connection Management para Serverless:** Singleton controlado de `MongoClient` fora do *handler* da Lambda com pool enxuto (`maxPoolSize: 5`), viabilizando o reaproveitamento de conexões ativas entre *warm starts*.
- **Idempotência no Consumo Kafka:** Garantia de consistência no MongoDB através de operações de escrita atômica com `$setOnInsert`, impedindo duplicidade no processamento.
- **Observabilidade Visual e Consulta Rápida:** Interface web para inspecionar tópicos do Kafka em tempo real e uma Lambda HTTP (`consulta-proposta`) para auditar o MongoDB direto pelo browser.

---

## 3. Visão Geral da Arquitetura

```text
       [ Interface Web Kafka (AKHQ) / Script ]
                         │
                         │ Publica Evento
                         ▼
     +-----------------------------------+
     |           Apache Kafka            |
     |   (credito.propostas.recebidas)   |
     +-----------------+-----------------+
                       │
                       │ Event Source Mapping (ESM) Batch
                       ▼
     +-----------------+-----------------+
     |            LAMBDA 1               |
     |     (processador-proposta)        |
     +--------+-----------------+--------+
              │                 │
              │ (1. Idempotent) │ (2. Direct Invoke: RequestResponse)
              ▼                 ▼
      +-------+-------+ +-------+-----------------+
      |    MongoDB    | |            LAMBDA 2     |
      |  (Collection: | |    (motor-regras-score) |
      |   propostas)  | +-------+-----------------+
      +---------------+         │
                                │ (3. Retorno: Score + Limite)
                                ▼
                        +-------+-----------------+
                        |   LAMBDA 1 (Conclusão)  |
                        |   - Atualiza Mongo      |
                        |   - Publica no Kafka    |
                        +-------+-----------------+
                                │
                                │ (4. Evento: credito.propostas.avaliadas)
                                ▼
     +-----------------------------------+
     |           Apache Kafka            |
     |   (credito.propostas.avaliadas)   |
     +-----------------+-----------------+
                       │
                       │ Event Source Mapping (ESM) Batch
                       ▼
     +-----------------+-----------------+
     |            LAMBDA 3               |
     |    (despachante-notificacao)      |
     +-----------------+-----------------+
                       │
                       │ (5. Trilha de Auditoria)
                       ▼
              +--------+--------+
              |     MongoDB     |
              |  (Collection:   |
              | auditoria_      |
              |  propostas)     |
              +--------+--------+
                       ▲
                       │ (6. Consulta HTTP / GET)
     +-----------------+-----------------+
     |            LAMBDA 4               |
     |       (consulta-proposta)         | ◄─── Browser / Postman / cURL
     +-----------------------------------+

```

---

## 4. Componentes de Arquitetura

### Lambda 1: Processador de Proposta (`processador-proposta`)

* **Trigger:** AWS Lambda Event Source Mapping (ESM) consumindo o tópico `credito.propostas.recebidas`.
* **Papel:** Decodifica lotes Kafka, registra o estado `EM_PROCESSAMENTO` no MongoDB com trava de idempotência, invoca síncronamente a Lambda 2 via AWS SDK v3, persiste a aprovação/rejeição e publica o resultado no tópico `credito.propostas.avaliadas`.

### Lambda 2: Motor de Regras de Score (`motor-regras-score`)

* **Trigger:** Chamada direta síncrona ponto a ponto (`RequestResponse`).
* **Papel:** Função determinística e isolada de banco de dados/rede. Avalia comprometimento de renda vs. valor solicitado, calcula score numérico, enquadra em faixa de risco (`BAIXO`, `MEDIO`, `ALTO`) e determina o limite liberado.

### Lambda 3: Despachante de Notificação (`despachante-notificacao`)

* **Trigger:** AWS Lambda Event Source Mapping (ESM) consumindo o tópico `credito.propostas.avaliadas`.
* **Papel:** Consome a decisão final de crédito e persiste uma trilha imutável na coleção `auditoria_propostas` no MongoDB.

### Lambda 4: Consulta de Proposta (`consulta-proposta`)

* **Trigger:** HTTP via AWS Lambda Function URL.
* **Papel:** Endpoint de leitura para validação manual. Recebe o `idProposta` via query param (`GET /?idProposta=PROP-1001`), busca no MongoDB os dados da proposta consolidada e o histórico de auditoria e devolve o JSON formatado.

---

## 5. Stack Tecnológica

| Tecnologia | Função no Ecossistema |
| --- | --- |
| **Node.js 20.x & TypeScript** | Runtime de execução com tipagem estática e empacotamento rápido via esbuild |
| **AWS Lambda & ESM** | Unidades de computação serverless elástica orientada a eventos |
| **LocalStack** | Emulador local dos serviços da AWS (Lambda, IAM, ESM, Function URL) |
| **Apache Kafka** | Broker de mensageria para streaming e desacoplamento de eventos |
| **AKHQ / Kafka UI** | Interface gráfica no navegador para visualizar tópicos, mensagens e partições |
| **MongoDB** | Banco NoSQL para persistência de propostas e histórico de auditoria |
| **Vitest** | Framework de testes unitários rápidos com cobertura de 100% dos cenários |

---

## 6. Pré-requisitos

Para rodar este projeto localmente:

* [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)
* [Node.js](https://nodejs.org/) (versão 20.x ou superior) e `npm`

---

## 7. Testes Automatizados (Unitários)

Os testes cobrem as regras de negócio de cada Lambda de forma isolada, mockando chamadas externas do SDK da AWS, KafkaJS e MongoDB:

* **Motor de Score (`motor-regras-score.spec.ts`):** Validação determinística das regras de cálculo de score, comprometimento de renda e cálculo de limites concedidos por faixa.
* **Processador de Propostas (`processador-proposta.spec.ts`):** Simulação de lotes Kafka, validação de escrita atômica no MongoDB, mock de invocação síncrona do AWS SDK e despacho no Kafka.
* **Despachante (`despachante-notificacao.spec.ts`):** Validação de persistência na coleção de auditoria e tratamento de lotes vazios ou corrompidos.
* **Consulta (`consulta-proposta.spec.ts`):** Validação de busca de proposta por ID, agregação com a coleção de auditoria e retorno de erro 404 para registros inexistentes.

```bash
# Executar todos os testes unitários
npm run test

# Executar com relatório de cobertura
npm run test:coverage

```

---

## 8. Como Subir e Executar Localmente

### Passo 1: Instalar Dependências e Compilar as Lambdas

```bash
npm install
npm run build

```

*Gera os pacotes compilados em `dist/*.zip` para carregamento imediato no LocalStack.*

### Passo 2: Subir os Containers do Ambiente

```bash
docker compose up -d

```

*Inicializa o Apache Kafka, o MongoDB, a UI do Kafka (AKHQ na porta 8080) e o LocalStack (porta 4566). O script de inicialização do LocalStack cria automaticamente as 4 Lambdas e os 2 gatilhos de Kafka.*

---

## 9. Como Fazer o Teste Manual Ponta a Ponta

### Cenário 1: Disparar Proposta via Interface Gráfica do Kafka (Browser)

1. Abra a interface web do Kafka no seu navegador: **`http://localhost:8080`**
2. Vá em **Topics** e selecione o tópico: **`credito.propostas.recebidas`**
3. Clique em **Produce message** e envie o payload JSON:
```json
{
  "idProposta": "PROP-2026-001",
  "documentoCpf": "12345678901",
  "valorSolicitado": 25000,
  "rendaMensal": 8000
}

```


4. Navegue até o tópico **`credito.propostas.avaliadas`** e veja a mensagem de score e limite gerada automaticamente pela esteira serverless!

---

### Cenário 2: Consultar o Resultado no MongoDB via Lambda HTTP (Browser ou cURL)

Para verificar como a proposta foi gravada no MongoDB sem precisar rodar comandos no banco:

Abra no navegador ou execute via terminal:

```bash
curl "http://localhost:4566/restapis/sua-url/consulta-proposta?idProposta=PROP-2026-001"

```

**Resposta JSON retornada:**

```json
{
  "proposta": {
    "idProposta": "PROP-2026-001",
    "documentoCpf": "12345678901",
    "valorSolicitado": 25000,
    "rendaMensal": 8000,
    "pontuacaoScore": 750,
    "faixaRisco": "BAIXO",
    "limiteAprovado": 25000,
    "status": "APROVADO",
    "criadoEm": "2026-11-09T10:00:00.000Z",
    "avaliadoEm": "2026-11-09T10:00:01.000Z"
  },
  "auditoria": [
    {
      "evento": "NOTIFICACAO_DESPACHADA",
      "registradoEm": "2026-11-09T10:00:02.000Z"
    }
  ]
}

```
