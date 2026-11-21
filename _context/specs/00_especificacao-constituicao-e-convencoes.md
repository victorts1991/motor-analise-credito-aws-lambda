# Spec 00 — Constituição e Convenções do Projeto

- **Status:** Aprovada
- **Objetivo:** definir as regras que valem para todo o projeto: princípios, fluxo de trabalho com specs, convenções de código, stack permitida, modelo de spec e definição de pronto. Toda outra spec e todo código seguem este documento.
- **Depende de:** [README](../../README.md), [ADR 0001](../../docs/adr/0001-invocacao-sincrona-entre-lambdas.md), [ADR 0002](../../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md), [ADR 0003](../../docs/adr/0003-emulador-local-localstack.md), [ADR 0004](../../docs/adr/0004-idempotencia-e-publicacao-no-kafka.md)

---

## 1. Regras de Negócio (RN)

Nesta spec, as regras são **regras do projeto**: valem para todas as outras specs e não podem ser contrariadas sem alterar esta spec antes.

- **RN-01 — A spec é a fonte da verdade.** Nenhum código é escrito sem uma spec com status `Aprovada`. Se o código precisar divergir da spec, a spec é corrigida primeiro e o código depois.
- **RN-02 — Uma spec por vez.** As specs são implementadas na ordem numérica, cada uma em uma branch própria e em um PR próprio.
- **RN-03 — Domínio puro.** O projeto `MotorCredito.Dominio` não referencia AWS SDK, MongoDB, Kafka nem qualquer biblioteca de I/O.
- **RN-04 — Idempotência obrigatória.** Todo processamento de mensagem do Kafka é idempotente, seguindo a [ADR 0004](../../docs/adr/0004-idempotencia-e-publicacao-no-kafka.md).
- **RN-05 — Clientes criados uma vez.** `MongoClient`, `IProducer` e `AmazonLambdaClient` são criados uma única vez por instância da Lambda, fora do handler, e reaproveitados entre invocações.
- **RN-06 — Dados sensíveis protegidos.** O CPF nunca aparece completo em log, mensagem de erro ou resposta HTTP. A regra de mascaramento é definida na spec 05.
- **RN-07 — Configuração por variável de ambiente.** Endpoints, nomes de tópicos, nomes de funções e credenciais vêm de variáveis de ambiente. Nada disso fica fixo no código. O arquivo `.env` nunca é commitado; o `.env.example` documenta todas as variáveis.
- **RN-08 — Execução 100% local.** O projeto roda com `docker compose` na máquina do desenvolvedor. Não existe pipeline de CI/CD nem deploy em conta AWS.
- **RN-09 — Rastreabilidade de testes.** Todo `RN`, `CA` e `EC` de uma spec é coberto por pelo menos um teste automatizado, e a seção **Testes** da spec indica qual teste cobre cada item. Quando o item depende de ferramenta externa ou do ambiente (scripts, Docker, LocalStack), ele pode ser coberto por uma **verificação manual**, descrita passo a passo na seção **Testes**, com o comando e o resultado esperado.
- **RN-10 — Decisões arquiteturais viram ADR.** Uma decisão que muda a arquitetura gera uma nova ADR em `docs/adr/`. ADRs aceitas não são editadas; uma decisão nova substitui a antiga com o status `Substituída pela ADR NNNN`.

---

## 2. Comportamento Esperado

Fluxo para implementar qualquer spec, seja por uma pessoa ou pelo Copilot.

### Antes de começar

1. Ler `_context/memory.md` para saber o estado atual do projeto.
2. Ler a spec inteira e todos os arquivos listados em **Depende de**.
3. Confirmar que todas as specs das quais ela depende estão com status `Implementada`.
4. Criar a branch `spec/NN-descricao-curta` (exemplo: `spec/06-motor-regras-score`).

### Durante a implementação

1. Implementar **somente** o que a spec descreve. O que estiver em **Non-Goals** não é feito.
2. Se faltar informação ou houver ambiguidade, **parar e perguntar**, em vez de inventar uma regra.
3. Escrever os testes junto com o código, seguindo a seção **Testes** da spec.

### Ao terminar (definição de pronto)

A spec só está pronta quando **todos** os itens abaixo forem verdadeiros:

- [ ] `dotnet build` termina sem erros e sem warnings.
- [ ] `dotnet test` passa por completo.
- [ ] Todo `CA` da spec foi verificado.
- [ ] Todo `RN`, `CA` e `EC` tem pelo menos um teste citado na seção **Testes**.
- [ ] O status da spec foi alterado para `Implementada`.
- [ ] O `_context/memory.md` foi atualizado.
- [ ] Uma entrada foi adicionada ao `_context/changelog.md`.
- [ ] Se algum comando ou passo de execução mudou, o `README.md` foi atualizado.

---

## 3. Arquivos e Onde Alterar

### Mapa do repositório

| Caminho | O que contém |
| --- | --- |
| `_context/specs/` | Specs numeradas de `00` a `10` |
| `_context/memory.md` | Estado atual do projeto (reescrito a cada spec) |
| `_context/changelog.md` | Histórico de mudanças (só acrescenta) |
| `docs/adr/` | Registros de decisão de arquitetura |
| `.github/copilot-instructions.md` | Regras globais para o Copilot, apontando para esta spec |
| `.github/instructions/` | Instruções do Copilot por tipo de arquivo |
| `.github/prompts/` | Prompts reutilizáveis |
| `src/MotorCredito.Dominio/` | Contratos, entidades, enums, regras puras e interfaces |
| `src/MotorCredito.Infraestrutura/` | Implementações de MongoDB, Kafka e cliente Lambda |
| `src/MotorCredito.Lambda.<Nome>/` | Uma pasta por Lambda, contendo só o handler e a composição das dependências |
| `tests/MotorCredito.<Projeto>.Tests/` | Um projeto de teste por projeto de `src/` que tenha lógica |
| `tests/MotorCredito.Arquitetura.Tests/` | Testes de arquitetura (regras de referência e pacotes proibidos) |
| `infra/localstack/init/ready.d/` | Scripts de criação de recursos no LocalStack |
| `scripts/` | Scripts de apoio (empacotamento, envio de eventos de teste) |

### Regras de referência entre projetos

```text
MotorCredito.Dominio                    (não referencia nenhum projeto)
MotorCredito.Infraestrutura          →  Dominio
MotorCredito.Lambda.MotorRegrasScore →  Dominio            (nunca Infraestrutura)
MotorCredito.Lambda.<demais>         →  Dominio, Infraestrutura
MotorCredito.<Projeto>.Tests         →  o projeto testado
MotorCredito.Arquitetura.Tests       →  nenhum projeto (lê os arquivos do repositório)
```

Uma Lambda **nunca** referencia outra Lambda.

---

## 4. Implementação Detalhada

### 4.1 Stack e versões

| Item | Valor |
| --- | --- |
| Target framework | `net10.0` |
| SDK | 10.x, fixado no `global.json` da raiz |
| Runtime da Lambda | `dotnet10`, arquitetura `x86_64` por padrão, configurável por `LAMBDA_ARCHITECTURE` ([ADR 0002](../../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md)) |
| Linguagem | Versão padrão de C# do .NET 10 (não definir `LangVersion`) |

Configurações obrigatórias em todos os `.csproj` (preferencialmente centralizadas em um `Directory.Build.props` na raiz):

- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`

### 4.2 Bibliotecas

**Permitidas** (outras só com alteração desta spec):

| Uso | Pacote |
| --- | --- |
| Lambda | `Amazon.Lambda.Core`, `Amazon.Lambda.Serialization.SystemTextJson`, `Amazon.Lambda.KafkaEvents`, `Amazon.Lambda.APIGatewayEvents` |
| Invocação entre Lambdas | `AWSSDK.Lambda` |
| Kafka (producer) | `Confluent.Kafka` |
| MongoDB | `MongoDB.Driver` |
| Testes | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `NSubstitute`, `Shouldly`, `coverlet.collector` |
| Testes de integração | `Testcontainers.MongoDb`, `Testcontainers.Kafka` |

**Proibidas:**

| Pacote | Motivo |
| --- | --- |
| `FluentAssertions` (v8 ou superior) | Licença comercial paga. Usar `Shouldly`. |
| `Moq` | Padronização: o projeto usa `NSubstitute`. |
| `Newtonsoft.Json` | O projeto usa `System.Text.Json`. |
| `Amazon.Lambda.Annotations` | Nesta POC o handler é escrito de forma explícita, para fins de estudo. |

### 4.3 Nomenclatura

- **Termos do domínio em português**, seguindo a linguagem do negócio: `Proposta`, `AvaliacaoScore`, `FaixaRisco`, `LimiteAprovado`.
- **Sufixos técnicos do .NET em inglês**, como é padrão na plataforma: `Request`, `Response`, `Client`, `Repository`, `Options`, `Exception`, `Async`, `Tests`.
  - Exemplos: `AvaliacaoScoreRequest`, `IMotorScoreClient`, `IPropostaRepository`, `PropostaNaoEncontradaException`.
- Classes, métodos e propriedades em `PascalCase`; parâmetros e variáveis locais em `camelCase`; campos privados em `_camelCase`.
- Métodos com I/O são assíncronos e terminam em `Async`.
- **JSON** (eventos Kafka, respostas HTTP) e **campos do MongoDB** em `camelCase` português: `idProposta`, `valorSolicitado`.
- **Valores de enum** serializados como texto em `MAIUSCULO_COM_UNDERSCORE`: `EM_PROCESSAMENTO`, `APROVADA`, `BAIXO`.
- **Variáveis de ambiente** em `MAIUSCULO_COM_UNDERSCORE`: `MONGODB_URI`, `KAFKA_BOOTSTRAP_SERVERS`.

### 4.4 Padrões de código

- **Handler das Lambdas:** a classe `Function` tem dois construtores:
  - um **sem parâmetros**, usado pelo runtime da Lambda, que monta as dependências reais;
  - um que **recebe as dependências por parâmetro**, usado pelos testes.
- **Sem container de injeção de dependência.** A composição é manual, no construtor sem parâmetros.
- **Serialização:** `System.Text.Json` com `JsonSerializerContext` (*source generator*) sempre que possível ([ADR 0002](../../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md)).
- **Valores monetários:** sempre `decimal`, nunca `double` ou `float`. No MongoDB, armazenados como `Decimal128`.
- **Datas:** sempre em UTC, obtidas por um `TimeProvider` injetado. Nunca usar `DateTime.Now` ou `DateTime.UtcNow` diretamente, para que os testes controlem o tempo. No JSON, formato ISO 8601.
- **Erros:** exceções de domínio específicas (herdando de uma base comum do projeto). Nenhuma exceção é engolida em silêncio; toda exceção capturada é registrada em log.
- **Logs:** estruturados, sem dados sensíveis (detalhes na spec 05).

### 4.5 Convenções de testes

- Padrão **Arrange / Act / Assert**, com as três partes separadas por uma linha em branco.
- Nome do teste: `Deve_<resultado>_Quando_<condicao>`. Exemplo: `Deve_retornar_404_Quando_proposta_nao_existe`.
- Testes de integração marcados com `[Trait("Category", "Integracao")]`, para o filtro do README funcionar.
- Testes de arquitetura marcados com `[Trait("Category", "Arquitetura")]`. Eles verificam as regras de referência entre projetos e a lista de pacotes proibidos lendo os arquivos `.slnx`, `.csproj` e `.props` do repositório, e são os únicos testes automatizados que podem ler arquivos do repositório.
- Um teste verifica um comportamento. Casos com a mesma lógica e dados diferentes usam `[Theory]` com `[InlineData]`.

### 4.6 Git

- **Branch:** `spec/NN-descricao-curta`.
- **Commits:** padrão [Conventional Commits](https://www.conventionalcommits.org/), em português e com uma linha. Tipos usados: `feat`, `fix`, `test`, `docs`, `refactor`, `chore`.
  - Exemplo: `feat: implementa cálculo de score no motor de regras`.
- **PR:** um por spec, com o título `Spec NN — <título da spec>` e a checklist da definição de pronto (seção 2) na descrição.

### 4.7 Modelo de spec

Toda spec de `01` a `10` segue este modelo, com estes títulos e nesta ordem. Uma seção que não se aplica continua presente, com o texto "Não se aplica".

```markdown
# Spec NN — <Título>

- **Status:** Rascunho | Aprovada | Implementada
- **Objetivo:** <uma ou duas frases>
- **Depende de:** <specs e ADRs, com link>

## 1. Regras de Negócio (RN)
- **RN-01 — <nome curto>.** <descrição>

## 2. Comportamento Esperado
<o que acontece, passo a passo, do ponto de vista de quem usa>

## 3. Arquivos e Onde Alterar
| Arquivo | Ação (criar / alterar) | O que muda |

## 4. Implementação Detalhada
<contratos, assinaturas, nomes de classes e sequência de passos; sem o corpo dos métodos>

## 5. Testes
| Teste | Tipo (unitário / integração / arquitetura / manual) | Cobre |
| `Deve_..._Quando_...` | unitário | RN-01, CA-02 |

## 6. Critérios de Aceitação (CA)
- **CA-01** — <condição verificável>

## 7. Non-Goals
- <o que esta spec não faz>

## 8. Edge Cases
- **EC-01 — <situação>.** <comportamento esperado>
```

Regras do modelo:

- Os identificadores (`RN-01`, `CA-01`, `EC-01`) recomeçam em cada spec e nunca são renumerados depois que a spec é aprovada. Item removido fica riscado, com o motivo.
- A seção **4. Implementação Detalhada** descreve o *quê* e o *onde*, não o código pronto.
- Verificações manuais recebem identificadores próprios (`VM-01`, `VM-02`...) e aparecem na tabela da seção **5. Testes** com o tipo `manual`.

### 4.8 `memory.md` e `changelog.md`

| | `_context/memory.md` | `_context/changelog.md` |
| --- | --- | --- |
| **Função** | Estado **atual** do projeto, lido antes de cada spec | **Histórico** do que mudou |
| **Atualização** | Reescrito ao fim de cada spec | Nova entrada adicionada **no topo** |
| **Tamanho** | Curto (até cerca de 60 linhas) | Cresce com o projeto |
| **Seções** | Estado das specs, próximo passo, decisões vigentes, ferramentas validadas, armadilhas descobertas, pendências | Entradas por data, agrupadas em Adicionado / Alterado / Corrigido / Removido |

O `memory.md` não repete o changelog nem as ADRs: ele aponta para eles.

---

## 5. Testes

Esta spec não gera código próprio, então não tem testes automatizados. Ela é verificada em toda revisão de PR, com as perguntas abaixo:

| Verificação | Cobre |
| --- | --- |
| O PR implementa uma única spec, numa branch `spec/NN-...`? | RN-02, CA-05 |
| `dotnet list package` não mostra nenhum pacote proibido? | CA-04 |
| O projeto `MotorCredito.Dominio` não tem referência de pacote de I/O? | RN-03 |
| A spec implementada tem a tabela de testes cobrindo todos os RN, CA e EC? | RN-09 |
| `memory.md` e `changelog.md` foram atualizados? | CA-03 |

---

## 6. Critérios de Aceitação (CA)

- **CA-01** — As specs `01` a `10` seguem o modelo da seção 4.7, com os 8 títulos na ordem definida.
- **CA-02** — O `.github/copilot-instructions.md` aponta para esta spec e para o `_context/memory.md`.
- **CA-03** — `_context/memory.md` e `_context/changelog.md` existem e seguem a seção 4.8.
- **CA-04** — Nenhum `.csproj` referencia um pacote da lista de proibidos.
- **CA-05** — Cada spec é entregue em um PR próprio, com a checklist da definição de pronto preenchida.

---

## 7. Non-Goals

- Não define regras de crédito (score, faixas, limites): spec 06.
- Não cria a solution nem os projetos .NET: spec 01.
- Não define contratos de eventos nem modelo de dados: spec 02.
- Não define a regra de mascaramento do CPF nem o formato dos logs: spec 05.
- Não trata de CI/CD, deploy em AWS, Native AOT nem autenticação na Function URL (ver seção 13 do README).

---

## 8. Edge Cases

- **EC-01 — Spec ambígua ou incompleta.** Quem implementa para e pergunta. A resposta é incorporada à spec antes de continuar o código.
- **EC-02 — Código precisa divergir da spec.** A spec é alterada primeiro, no mesmo PR, com a mudança explicada na descrição (RN-01).
- **EC-03 — Necessidade de uma biblioteca fora da lista.** A seção 4.2 desta spec é atualizada antes de adicionar o pacote.
- **EC-04 — Spec depende de outra ainda não implementada.** A implementação não começa até a dependência estar `Implementada`.
- **EC-05 — Uma decisão de arquitetura muda.** Cria-se uma nova ADR que substitui a anterior; a ADR antiga só tem o status alterado (RN-10).
- **EC-06 — Conflito entre esta spec e outra.** Esta spec prevalece, e a outra é corrigida.