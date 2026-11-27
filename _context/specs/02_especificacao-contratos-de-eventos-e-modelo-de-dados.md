# Spec 02 — Contratos de Eventos e Modelo de Dados

- **Status:** Implementada
- **Objetivo:** definir, no projeto `MotorCredito.Dominio`, os contratos que trafegam entre as Lambdas (eventos Kafka e invocação do motor), as entidades persistidas no MongoDB, as regras de validação de cada contrato e um único contexto de serialização JSON. As specs 04 a 09 usam estes tipos sem redefini-los.
- **Depende de:** [Spec 00](./00_especificacao-constituicao-e-convencoes.md), [Spec 01](./01_especificacao-estrutura-da-solution-e-empacotamento-dotnet.md), [ADR 0001](../../docs/adr/0001-invocacao-sincrona-entre-lambdas.md), [ADR 0002](../../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md), [ADR 0004](../../docs/adr/0004-idempotencia-e-publicacao-no-kafka.md)

---

## 1. Regras de Negócio (RN)

- **RN-01 — Contratos e entidades no domínio.** Os quatro contratos (`PropostaRecebidaEvent`, `PropostaAvaliadaEvent`, `AvaliacaoScoreRequest`, `AvaliacaoScoreResponse`), as duas entidades (`Proposta`, `RegistroAuditoria`) e os quatro enums ficam em `MotorCredito.Dominio`. Contratos e entidades são `sealed record` imutáveis (sem `set` público; só `init` ou parâmetros posicionais). O projeto continua sem `PackageReference` e sem `ProjectReference`: `System.Text.Json` vem do próprio framework.
- **RN-02 — JSON em camelCase, sensível a maiúsculas.** Os nomes das propriedades no JSON são exatamente os da seção 4.2 (camelCase português). Um campo escrito com outra caixa (`IdProposta`) não é reconhecido e conta como ausente.
- **RN-03 — Enums como texto estrito.** Os enums são serializados como texto `MAIUSCULO_COM_UNDERSCORE` (tabela da seção 4.3). Na leitura, só o texto exato é aceito. São rejeitados: outra caixa (`aprovada`), o nome C# (`Aprovada`), números (`1`), espaços antes ou depois (`" APROVADA"`), listas separadas por vírgula (`"EM_PROCESSAMENTO, APROVADA"`) e qualquer valor desconhecido. Nenhum enum tem membro com valor `0`. Um enum com valor fora do contrato (como `default`) não pode ser serializado.
- **RN-04 — Campos obrigatórios, sem `null` e sem duplicados.** Todos os campos dos quatro contratos são obrigatórios. Um campo ausente, com `null` ou repetido no mesmo objeto JSON torna o payload inválido.
- **RN-05 — Valores monetários.** Valores monetários são `decimal` no C# e número no JSON. Um número enviado como texto (`"25000"`) é rejeitado, assim como um número que não cabe em `decimal` (`1e30`). Valores monetários têm no máximo **duas casas decimais significativas**; a verificação é pelo valor, não pela escala (`25000.500` é aceito porque vale `25000.50`).
- **RN-06 — Datas em UTC com fuso explícito.** As datas são `DateTimeOffset` no C#. No JSON, são escritas sempre como `yyyy-MM-ddTHH:mm:ss.fffZ` (UTC, milissegundos, sufixo `Z`). Na leitura, só é aceito texto ISO 8601 com data, hora e fuso UTC explícito (`Z` ou `+00:00`). Uma data sem fuso, com outro fuso, só com a data, numérica ou inválida é rejeitada.
- **RN-07 — Regras da proposta recebida.**
  - `idProposta`: de 1 a 64 caracteres; só letras e dígitos ASCII, `-` e `_`.
  - `documentoCpf`: exatamente 11 dígitos ASCII (`0`–`9`), sem pontuação. Os dígitos verificadores **não** são validados.
  - `valorSolicitado` e `rendaMensal`: maiores que zero e com até duas casas decimais (RN-05).
- **RN-08 — Regras da proposta avaliada.**
  - `idProposta`: mesma regra da RN-07.
  - `decisao` e `faixaRisco`: valores definidos nos enums (RN-03).
  - `pontuacaoScore`: inteiro de `0` a `1000`, inclusive.
  - `limiteAprovado`: maior ou igual a zero, com até duas casas decimais. Se a decisão for `APROVADA`, é maior que zero. Se for `REPROVADA`, é exatamente zero.
  - `avaliadoEm`: em UTC (`Offset` igual a zero).
- **RN-09 — Leitura única e sem exceção.** Os consumidores leem um payload do Kafka por uma única porta de entrada (`LeitorDeContratos`), que junta a desserialização e a validação. A leitura nunca lança exceção por conteúdo inválido: devolve um resultado com o valor ou com a lista de erros. A validação devolve **todos** os erros encontrados, na ordem dos campos do contrato.
- **RN-10 — Proteção do CPF.** O CPF existe só em `PropostaRecebidaEvent` e `Proposta`. `AvaliacaoScoreRequest`, `AvaliacaoScoreResponse`, `PropostaAvaliadaEvent` e `RegistroAuditoria` não têm campo de CPF. O `ToString()` de `PropostaRecebidaEvent` e de `Proposta` mostra `DocumentoCpf = ***` no lugar do valor. Nenhum erro de leitura ou validação (campo ou motivo) contém valores do payload.
- **RN-11 — Tolerância a campos novos.** Campos desconhecidos no JSON são ignorados, para que um produtor possa adicionar um campo opcional sem quebrar os consumidores. Uma mudança incompatível (remover, renomear ou mudar o tipo de um campo) exige uma nova spec.
- **RN-12 — Contexto de serialização único.** Existe um único `JsonSerializerContext` (*source generator*), `MotorCreditoJsonContext`, com as opções da seção 4.5 e os metadados dos quatro contratos e dos quatro enums. Todo código que serializa ou desserializa esses tipos usa esse contexto.
- **RN-13 — Projeto de testes do domínio.** Esta spec cria o projeto `MotorCredito.Dominio.Tests`, que referencia só `MotorCredito.Dominio`. A solution passa a ter 12 projetos: 6 em `src/` e 6 em `tests/`.

---

## 2. Comportamento Esperado

Do ponto de vista das Lambdas que vão usar estes tipos (specs 06 a 09):

1. O `processador-proposta` recebe os bytes de uma mensagem de `credito.propostas.recebidas`, chama `LeitorDeContratos.LerPropostaRecebida` e recebe:
   - `Valido = true` e o `PropostaRecebidaEvent` preenchido; ou
   - `Valido = false`, `Valor = null` e a lista de `ErroValidacao` (campo e motivo), sem nenhum valor do payload.
2. O `processador-proposta` monta um `AvaliacaoScoreRequest` (sem CPF), serializa com o `MotorCreditoJsonContext` e invoca o `motor-regras-score`, que responde um `AvaliacaoScoreResponse` serializado com o mesmo contexto.
3. O `processador-proposta` monta um `PropostaAvaliadaEvent`, carimba `avaliadoEm` com o `TimeProvider` e publica o JSON com chave `idProposta`.
4. O `despachante-notificacao` lê a mensagem com `LeitorDeContratos.LerPropostaAvaliada`, com o mesmo tipo de resultado do item 1.
5. As entidades `Proposta` e `RegistroAuditoria` representam os documentos das coleções `propostas` e `auditoria_propostas`. O mapeamento para BSON e os índices ficam para a spec 04 (seção 4.7).

Exemplo do contrato `PropostaAvaliadaEvent` serializado (é o JSON exato esperado no CA-04):

```json
{"idProposta":"PROP-2026-001","decisao":"APROVADA","pontuacaoScore":750,"faixaRisco":"BAIXO","limiteAprovado":25000,"avaliadoEm":"2026-11-09T10:00:01.200Z"}
```

---

## 3. Arquivos e Onde Alterar

| Arquivo | Ação | O que muda |
| --- | --- | --- |
| `src/MotorCredito.Dominio/Propostas/StatusProposta.cs` | criar | Enum `StatusProposta` |
| `src/MotorCredito.Dominio/Propostas/DecisaoCredito.cs` | criar | Enum `DecisaoCredito` |
| `src/MotorCredito.Dominio/Propostas/FaixaRisco.cs` | criar | Enum `FaixaRisco` |
| `src/MotorCredito.Dominio/Propostas/Proposta.cs` | criar | Entidade `Proposta` |
| `src/MotorCredito.Dominio/Auditoria/TipoEventoAuditoria.cs` | criar | Enum `TipoEventoAuditoria` |
| `src/MotorCredito.Dominio/Auditoria/RegistroAuditoria.cs` | criar | Entidade `RegistroAuditoria` |
| `src/MotorCredito.Dominio/Eventos/PropostaRecebidaEvent.cs` | criar | Contrato do tópico `credito.propostas.recebidas` |
| `src/MotorCredito.Dominio/Eventos/PropostaAvaliadaEvent.cs` | criar | Contrato do tópico `credito.propostas.avaliadas` |
| `src/MotorCredito.Dominio/Score/AvaliacaoScoreRequest.cs` | criar | Entrada do motor de score |
| `src/MotorCredito.Dominio/Score/AvaliacaoScoreResponse.cs` | criar | Saída do motor de score |
| `src/MotorCredito.Dominio/Serializacao/EnumEstritoConverter.cs` | criar | Conversor de enum da RN-03 |
| `src/MotorCredito.Dominio/Serializacao/DataUtcConverter.cs` | criar | Conversor de data da RN-06 |
| `src/MotorCredito.Dominio/Serializacao/MotorCreditoJsonContext.cs` | criar | Contexto de serialização da RN-12 |
| `src/MotorCredito.Dominio/Contratos/ErroValidacao.cs` | criar | Erro de leitura ou validação |
| `src/MotorCredito.Dominio/Contratos/ResultadoLeitura.cs` | criar | Resultado do `LeitorDeContratos` |
| `src/MotorCredito.Dominio/Contratos/RegrasDeCampo.cs` | criar | Regras de campo compartilhadas (`internal`) |
| `src/MotorCredito.Dominio/Contratos/ValidadorPropostaRecebida.cs` | criar | Validação da RN-07 |
| `src/MotorCredito.Dominio/Contratos/ValidadorPropostaAvaliada.cs` | criar | Validação da RN-08 |
| `src/MotorCredito.Dominio/Contratos/LeitorDeContratos.cs` | criar | Porta de leitura da RN-09 |
| `tests/MotorCredito.Dominio.Tests/MotorCredito.Dominio.Tests.csproj` | criar | Projeto de teste do domínio |
| `tests/MotorCredito.Dominio.Tests/Payloads.cs` | criar | Payloads JSON de teste e utilitários para remover/alterar campos |
| `tests/MotorCredito.Dominio.Tests/Serializacao/*.cs` | criar | Testes de JSON, enums e datas |
| `tests/MotorCredito.Dominio.Tests/Contratos/*.cs` | criar | Testes do leitor e dos validadores |
| `tests/MotorCredito.Dominio.Tests/Modelo/*.cs` | criar | Testes de proteção do CPF e de forma dos tipos |
| `MotorAnaliseCredito.slnx` | alterar | Adiciona `MotorCredito.Dominio.Tests` na pasta `/tests/` |
| `tests/MotorCredito.Arquitetura.Tests/SolutionTests.cs` | alterar | Contagem passa de 11 (6 + 5) para 12 (6 + 6) |
| `tests/MotorCredito.Arquitetura.Tests/ReferenciasEntreProjetosTests.cs` | alterar | Novo teste: `Dominio.Tests` referencia só `Dominio` |

Não muda: `Directory.Packages.props` (nenhum pacote novo), `.env.example`, `README.md` e os projetos de Lambda e de infraestrutura.

---

## 4. Implementação Detalhada

### 4.1 Catálogo de canais

| Canal | Contrato | Produtor | Consumidor | Chave |
| --- | --- | --- | --- | --- |
| Tópico `credito.propostas.recebidas` | `PropostaRecebidaEvent` | AKHQ / script | `processador-proposta` | `idProposta` (texto UTF-8) |
| Invocação `RequestResponse` da Lambda `motor-regras-score` | `AvaliacaoScoreRequest` → `AvaliacaoScoreResponse` | `processador-proposta` | `motor-regras-score` | — |
| Tópico `credito.propostas.avaliadas` | `PropostaAvaliadaEvent` | `processador-proposta` | `despachante-notificacao` | `idProposta` (texto UTF-8) |
| Coleção `propostas` | `Proposta` | `processador-proposta` | `consulta-proposta` | índice único `idProposta` |
| Coleção `auditoria_propostas` | `RegistroAuditoria` | `despachante-notificacao` | `consulta-proposta` | índice único `idProposta` + `evento` |

- Os nomes de tópicos e coleções vêm de variáveis de ambiente (spec 00, RN-07), criadas na spec 03. Esta spec não coloca esses nomes no código.
- O valor da mensagem Kafka é o JSON do contrato em UTF-8, sem envelope.
- O `idProposta` do payload é a fonte da verdade. O que fazer se a chave da mensagem for diferente fica para a spec 07.

### 4.2 Contratos

Todos são `sealed record` **posicionais**, nesta ordem de parâmetros. A ordem dos parâmetros define a ordem dos campos no JSON e a ordem dos erros de validação.

**`PropostaRecebidaEvent`** (namespace `MotorCredito.Dominio.Eventos`)

| Propriedade C# | Campo JSON | Tipo | Regra |
| --- | --- | --- | --- |
| `IdProposta` | `idProposta` | `string` | RN-07 |
| `DocumentoCpf` | `documentoCpf` | `string` | RN-07, RN-10 |
| `ValorSolicitado` | `valorSolicitado` | `decimal` | RN-05, RN-07 |
| `RendaMensal` | `rendaMensal` | `decimal` | RN-05, RN-07 |

**`AvaliacaoScoreRequest`** (namespace `MotorCredito.Dominio.Score`): `IdProposta` (`string`), `ValorSolicitado` (`decimal`), `RendaMensal` (`decimal`). Não tem CPF: o cálculo não depende dele. O `idProposta` serve só para correlacionar logs. A validação da entrada do motor é da spec 06.

**`AvaliacaoScoreResponse`** (namespace `MotorCredito.Dominio.Score`): `IdProposta` (`string`), `Decisao` (`DecisaoCredito`), `PontuacaoScore` (`int`), `FaixaRisco` (`FaixaRisco`), `LimiteAprovado` (`decimal`). Não tem data: o motor não lê relógio, e quem carimba `avaliadoEm` é o processador.

**`PropostaAvaliadaEvent`** (namespace `MotorCredito.Dominio.Eventos`)

| Propriedade C# | Campo JSON | Tipo | Regra |
| --- | --- | --- | --- |
| `IdProposta` | `idProposta` | `string` | RN-08 |
| `Decisao` | `decisao` | `DecisaoCredito` | RN-03, RN-08 |
| `PontuacaoScore` | `pontuacaoScore` | `int` | RN-08 |
| `FaixaRisco` | `faixaRisco` | `FaixaRisco` | RN-03, RN-08 |
| `LimiteAprovado` | `limiteAprovado` | `decimal` | RN-05, RN-08 |
| `AvaliadoEm` | `avaliadoEm` | `DateTimeOffset` | RN-06, RN-08 |

Sem CPF (RN-10): o despachante não precisa dele.

**Proteção do CPF no `ToString()` (RN-10):** o `ToString()` gerado automaticamente pelo `record` imprime todas as propriedades, então `logger.Log(evento)` vazaria o CPF. `PropostaRecebidaEvent` e `Proposta` declaram o método `private bool PrintMembers(StringBuilder builder)` (a assinatura que o compilador usa em `sealed record`) e escrevem `DocumentoCpf = ***` no lugar do valor, mantendo as demais propriedades.

### 4.3 Enums

Namespaces: `StatusProposta`, `DecisaoCredito` e `FaixaRisco` em `MotorCredito.Dominio.Propostas`; `TipoEventoAuditoria` em `MotorCredito.Dominio.Auditoria`.

| Enum | Membro C# | Valor numérico | Texto no JSON |
| --- | --- | --- | --- |
| `StatusProposta` | `EmProcessamento` | 1 | `EM_PROCESSAMENTO` |
| | `Aprovada` | 2 | `APROVADA` |
| | `Reprovada` | 3 | `REPROVADA` |
| `DecisaoCredito` | `Aprovada` | 1 | `APROVADA` |
| | `Reprovada` | 2 | `REPROVADA` |
| `FaixaRisco` | `Baixo` | 1 | `BAIXO` |
| | `Medio` | 2 | `MEDIO` |
| | `Alto` | 3 | `ALTO` |
| `TipoEventoAuditoria` | `DecisaoRegistrada` | 1 | `DECISAO_REGISTRADA` |

- O texto de cada membro é declarado com `[JsonStringEnumMemberName("...")]`.
- Os valores começam em 1 para que `default` (0) nunca seja um valor válido e um campo esquecido apareça como erro.
- `DecisaoCredito` é separado de `StatusProposta` porque uma decisão nunca pode ser `EM_PROCESSAMENTO`.

### 4.4 Conversores

**`EnumEstritoConverter<TEnum>`** (`MotorCredito.Dominio.Serializacao`): aplicado em cada enum com `[JsonConverter(typeof(EnumEstritoConverter<TEnum>))]`.

> **Armadilha verificada no .NET 10:** o `JsonStringEnumConverter` padrão, mesmo com `allowIntegerValues: false`, ignora espaços antes e depois do texto e aceita listas separadas por vírgula mesmo em enum sem `[Flags]`. Com ele, `"EM_PROCESSAMENTO, APROVADA"` vira `StatusProposta.Reprovada` (1 | 2 = 3) sem erro. Por isso o conversor da RN-03 é obrigatório.

Comportamento:

| Situação na leitura | Resultado |
| --- | --- |
| Token não é texto (número, `true`, objeto…) | `JsonException` |
| Texto com espaço no início ou no fim, ou com vírgula | `JsonException` |
| Texto diferente, em qualquer caixa, do `JsonStringEnumMemberName` de um membro | `JsonException` |
| Texto exato de um membro | O membro correspondente |

| Situação na escrita | Resultado |
| --- | --- |
| Membro definido | O texto da tabela 4.3 |
| Valor não definido (como `default`) | `JsonException` |

A implementação pode usar o `JsonStringEnumConverter<TEnum>` internamente, desde que faça as verificações acima antes e depois dele.

**`DataUtcConverter`** (`MotorCredito.Dominio.Serializacao`): `JsonConverter<DateTimeOffset>`, registrado no contexto (seção 4.5).

> **Armadilha verificada no .NET 10:** o conversor padrão aceita uma data sem fuso (`"2026-11-09T10:00:01"`) e a interpreta no **fuso da máquina**. O mesmo JSON vira `-03:00` no notebook e UTC na Lambda, sem erro. O padrão também escreve `+00:00` e até 7 casas de fração, e não o formato do README.

- **Escrita:** converte para UTC e escreve `yyyy-MM-ddTHH:mm:ss.fffZ` com `CultureInfo.InvariantCulture`. Frações abaixo de milissegundo são descartadas; é a mesma precisão do tipo `Date` do MongoDB.
- **Leitura:** exige um token de texto, com `T` na posição 10, terminado em `Z` ou `+00:00`, e que o `DateTimeOffset.TryParse` com `InvariantCulture` aceite. Caso contrário, `JsonException`.

### 4.5 `MotorCreditoJsonContext`

`public sealed partial class MotorCreditoJsonContext : JsonSerializerContext`, em `MotorCredito.Dominio.Serializacao`, com:

| Opção de `[JsonSourceGenerationOptions]` | Valor | Por quê |
| --- | --- | --- |
| `PropertyNamingPolicy` | `JsonKnownNamingPolicy.CamelCase` | RN-02 |
| `RespectRequiredConstructorParameters` | `true` | Sem isso, um parâmetro ausente vira `0`/`null` em silêncio (RN-04) |
| `RespectNullableAnnotations` | `true` | Sem isso, `"documentoCpf": null` é aceito numa `string` não anulável (RN-04) |
| `AllowDuplicateProperties` | `false` | Sem isso, `{"idProposta":"A","idProposta":"B"}` é aceito e o último vence (RN-04) |
| `NumberHandling` | `JsonNumberHandling.Strict` | Rejeita número como texto (RN-05) |
| `Converters` | `[typeof(DataUtcConverter)]` | RN-06 |

Não definir `PropertyNameCaseInsensitive` (o padrão `false` atende a RN-02) nem `UnmappedMemberHandling` (o padrão `Skip` atende a RN-11).

`[JsonSerializable]` para: `PropostaRecebidaEvent`, `PropostaAvaliadaEvent`, `AvaliacaoScoreRequest`, `AvaliacaoScoreResponse`, `StatusProposta`, `DecisaoCredito`, `FaixaRisco` e `TipoEventoAuditoria`.

As Lambdas que receberem tipos de outros pacotes (como o `KafkaEvent`) terão um contexto próprio para o handler (specs 07 a 09); o payload continua sendo lido por este.

### 4.6 Leitura e validação

Namespace `MotorCredito.Dominio.Contratos`.

- **`ErroValidacao`** — `sealed record ErroValidacao(string Campo, string Motivo)`.
  - Erro de validação: `Campo` é o nome JSON do campo (`"documentoCpf"`).
  - Erro de desserialização: `Campo` é o `JsonException.Path` (`"$.valorSolicitado"`), ou `"$"` se não houver caminho.
  - `Motivo` é um texto fixo em português, que nunca inclui o valor recebido nem a `Message` da `JsonException`.
- **`ResultadoLeitura<T>`** (`where T : class`) — `sealed record` com `T? Valor`, `IReadOnlyList<ErroValidacao> Erros` e `bool Valido` (verdadeiro quando `Valor` não é nulo e `Erros` está vazio). Construtores e fábricas `Sucesso`/`Falha` são `internal`; só o leitor cria resultados.
- **`ValidadorPropostaRecebida.Validar(PropostaRecebidaEvent)`** e **`ValidadorPropostaAvaliada.Validar(PropostaAvaliadaEvent)`** — classes `static`, métodos puros que devolvem `IReadOnlyList<ErroValidacao>` (vazia quando válido) com todos os erros, na ordem dos campos. No máximo um erro por campo. Lançam `ArgumentNullException` só se o argumento for `null`. Os enums são validados com `Enum.IsDefined`, porque um objeto criado no código pode ter `default`.
- **`RegrasDeCampo`** — classe `internal static` com as regras compartilhadas (`idProposta`, CPF, valor positivo, casas decimais) e as constantes 64, 11, 2, 0 e 1000, para não repetir as regras nos dois validadores.
- **`LeitorDeContratos`** — classe `static` com:
  - `ResultadoLeitura<PropostaRecebidaEvent> LerPropostaRecebida(ReadOnlySpan<byte> utf8Json)`
  - `ResultadoLeitura<PropostaAvaliadaEvent> LerPropostaAvaliada(ReadOnlySpan<byte> utf8Json)`

  Sequência: desserializa com o `MotorCreditoJsonContext` → se der `JsonException`, devolve falha com um único erro (`Path`) → se o resultado for `null`, devolve falha com o erro `("$", "payload vazio ou null")` → valida → devolve sucesso ou falha com os erros do validador.

  > **Armadilha verificada no .NET 10:** o payload `null` (o literal JSON) é desserializado **sem exceção** e devolve `null`. Quem só captura `JsonException` deixa esse caso passar.

### 4.7 Entidades e modelo de persistência

**`Proposta`** (`MotorCredito.Dominio.Propostas`) — `sealed record` com propriedades `init`. Obrigatórias (`required`): `IdProposta`, `DocumentoCpf`, `ValorSolicitado`, `RendaMensal`, `Status`, `CriadoEm`. Opcionais (anuláveis): `PontuacaoScore` (`int?`), `FaixaRisco` (`FaixaRisco?`), `LimiteAprovado` (`decimal?`), `AvaliadoEm` (`DateTimeOffset?`), `PublicadoEm` (`DateTimeOffset?`). O `ToString()` segue a seção 4.2.

**`RegistroAuditoria`** (`MotorCredito.Dominio.Auditoria`) — `sealed record` com propriedades `required init`: `IdProposta`, `Evento` (`TipoEventoAuditoria`), `Decisao`, `PontuacaoScore`, `FaixaRisco`, `LimiteAprovado`, `AvaliadoEm`, `RegistradoEm`. É uma fotografia imutável da decisão no momento do registro.

Modelo dos documentos, **implementado na spec 04** (mapeamento BSON, criação de índices e repositórios). Esta spec só o define:

| Coleção | Campo | Tipo BSON | Observação |
| --- | --- | --- | --- |
| `propostas` | `_id` | `ObjectId` | Gerado pelo MongoDB |
| | `idProposta` | `String` | Índice único (ADR 0004) |
| | `documentoCpf` | `String` | CPF completo; mascarado só na saída (spec 05/09) |
| | `valorSolicitado`, `rendaMensal` | `Decimal128` | Spec 00, 4.4 |
| | `status` | `String` | Texto da tabela 4.3 |
| | `pontuacaoScore` | `Int32` ou ausente | Ausente enquanto `EM_PROCESSAMENTO` |
| | `faixaRisco` | `String` ou ausente | Idem |
| | `limiteAprovado` | `Decimal128` ou ausente | Idem |
| | `criadoEm`, `avaliadoEm`, `publicadoEm` | `Date` (UTC, milissegundos) | `avaliadoEm` e `publicadoEm` ausentes até acontecerem |
| `auditoria_propostas` | `_id` | `ObjectId` | |
| | `idProposta` + `evento` | `String` + `String` | Índice único composto (ADR 0004) |
| | `decisao`, `faixaRisco` | `String` | Texto da tabela 4.3 |
| | `pontuacaoScore` | `Int32` | |
| | `limiteAprovado` | `Decimal128` | |
| | `avaliadoEm`, `registradoEm` | `Date` | |

As transições de estado (`EM_PROCESSAMENTO → APROVADA | REPROVADA`, preenchimento de `publicadoEm`) e as invariantes entre `status` e os campos opcionais são regras da spec 04.

### 4.8 Projeto `MotorCredito.Dominio.Tests`

- `IsTestProject` = `true`. Pacotes: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Shouldly`, `coverlet.collector` (NSubstitute não é necessário: não há dependências para simular). `<Using Include="Xunit" />`.
- Referencia só `src/MotorCredito.Dominio`.
- Os testes não leem arquivos nem acessam rede. Reflexão sobre os tipos do domínio é permitida.
- `Payloads.cs` guarda o JSON do Cenário 1 do README e o JSON da seção 2, além de métodos que removem um campo, trocam um campo por `null` ou trocam o valor de um campo (via `System.Text.Json.Nodes`).
- Valores `decimal` em `[Theory]` usam `[MemberData]` com `TheoryData<decimal>` (atributos não aceitam `decimal`), mantendo o sufixo `m`.

---

## 5. Testes

### Testes automatizados

Projeto `MotorCredito.Dominio.Tests`, exceto onde indicado.

| Teste | Classe | Tipo | Cobre |
| --- | --- | --- | --- |
| `Deve_gerar_json_em_camelCase_Quando_PropostaRecebidaEvent_e_serializado` | `ContratosJsonTests` | unitário | RN-02, RN-12 |
| `Deve_gerar_json_do_contrato_Quando_PropostaAvaliadaEvent_e_serializado` | `ContratosJsonTests` | unitário | RN-02, RN-03, RN-06, RN-12, CA-04 |
| `Deve_gerar_json_do_contrato_Quando_AvaliacaoScoreRequest_e_serializado` | `ContratosJsonTests` | unitário | RN-02, RN-10, RN-12 |
| `Deve_gerar_json_do_contrato_Quando_AvaliacaoScoreResponse_e_serializado` | `ContratosJsonTests` | unitário | RN-02, RN-03, RN-12 |
| `Deve_preservar_valores_Quando_AvaliacaoScoreResponse_faz_ida_e_volta` | `ContratosJsonTests` | unitário | RN-05, RN-12 |
| `Deve_preservar_valores_Quando_PropostaAvaliadaEvent_faz_ida_e_volta` | `ContratosJsonTests` | unitário | RN-05, RN-06, RN-12 |
| `Deve_ter_metadados_gerados_Quando_tipo_e_contrato` (`[Theory]`, 8 tipos) | `ContratosJsonTests` | unitário | RN-12 |
| `Deve_usar_o_texto_do_contrato_Quando_enum_e_serializado` (`[Theory]`, os 9 membros) | `EnumsJsonTests` | unitário | RN-03 |
| `Deve_rejeitar_decisao_Quando_texto_nao_e_exatamente_o_do_contrato` (`[Theory]`: `aprovada`, `Aprovada`, `APROVADO`, `EM_PROCESSAMENTO`, `1`, `0`) | `EnumsJsonTests` | unitário | RN-03, EC-08, EC-10 |
| `Deve_rejeitar_faixaRisco_Quando_texto_nao_e_exatamente_o_do_contrato` (`[Theory]`: `baixo`, `Medio`, `MÉDIO`, `CRITICO`, `2`) | `EnumsJsonTests` | unitário | RN-03, EC-08 |
| `Deve_rejeitar_enum_Quando_texto_tem_espaco_ou_virgula` (`[Theory]` em `StatusProposta`: `" APROVADA"`, `"APROVADA "`, `"EM_PROCESSAMENTO, APROVADA"`, `"APROVADA,REPROVADA"`, `"APROVADA, APROVADA"`) | `EnumsJsonTests` | unitário | RN-03, EC-09 |
| `Deve_lancar_excecao_Quando_evento_com_enum_zero_e_serializado` | `EnumsJsonTests` | unitário | RN-03, EC-11 |
| `Deve_nao_ter_membro_zero_Quando_enum_e_do_contrato` (`[Theory]`, 4 enums) | `EnumsJsonTests` | unitário | RN-03 |
| `Deve_escrever_em_utc_com_milissegundos_Quando_data_e_serializada` (`[Theory]`: com ms, sem ms, em `-03:00`, com ticks abaixo de ms) | `DatasJsonTests` | unitário | RN-06, EC-17, EC-18 |
| `Deve_aceitar_data_Quando_fuso_utc_e_explicito` (`[Theory]`: `...200Z`, `...01Z`, `...200+00:00`) | `DatasJsonTests` | unitário | RN-06 |
| `Deve_rejeitar_data_Quando_fuso_utc_nao_e_explicito_ou_formato_e_invalido` (`[Theory]`: sem fuso, `-03:00`, só data, data impossível, texto vazio, número) | `DatasJsonTests` | unitário | RN-06, EC-16, EC-17 |
| `Deve_ler_proposta_Quando_payload_e_o_exemplo_do_README` | `LeitorDeContratosTests` | unitário | RN-09, CA-03 |
| `Deve_ler_proposta_avaliada_Quando_payload_respeita_o_contrato` | `LeitorDeContratosTests` | unitário | RN-09 |
| `Deve_falhar_sem_excecao_Quando_payload_e_o_literal_null` | `LeitorDeContratosTests` | unitário | RN-09, EC-01 |
| `Deve_falhar_sem_excecao_Quando_payload_nao_e_um_objeto_json_valido` (`[Theory]`: vazio, `{`, objeto cortado, array, texto, não JSON) | `LeitorDeContratosTests` | unitário | RN-09, EC-02 |
| `Deve_falhar_Quando_campo_obrigatorio_da_proposta_recebida_esta_ausente` (`[Theory]`, 4 campos) | `LeitorDeContratosTests` | unitário | RN-04, EC-03 |
| `Deve_falhar_Quando_campo_obrigatorio_da_proposta_avaliada_esta_ausente` (`[Theory]`, 6 campos) | `LeitorDeContratosTests` | unitário | RN-04, EC-03 |
| `Deve_falhar_Quando_campo_obrigatorio_vem_null` (`[Theory]`, 4 campos; erro no `$.<campo>`) | `LeitorDeContratosTests` | unitário | RN-04, EC-04 |
| `Deve_falhar_Quando_nomes_dos_campos_estao_em_PascalCase` | `LeitorDeContratosTests` | unitário | RN-02, EC-05 |
| `Deve_ignorar_campo_desconhecido_Quando_payload_tem_campo_a_mais` | `LeitorDeContratosTests` | unitário | RN-11, EC-06 |
| `Deve_falhar_Quando_campo_aparece_duas_vezes` | `LeitorDeContratosTests` | unitário | RN-04, EC-07 |
| `Deve_falhar_Quando_valor_monetario_vem_como_texto` (`[Theory]`, 2 campos) | `LeitorDeContratosTests` | unitário | RN-05, EC-12 |
| `Deve_falhar_Quando_valor_monetario_excede_o_limite_do_decimal` | `LeitorDeContratosTests` | unitário | RN-05, EC-15 |
| `Deve_falhar_Quando_proposta_avaliada_reprovada_traz_limite` | `LeitorDeContratosTests` | unitário | RN-08, RN-09 |
| `Deve_nao_expor_o_cpf_Quando_leitura_falha` (`[Theory]` com payloads inválidos que contêm CPF) | `LeitorDeContratosTests` | unitário | RN-09, RN-10 |
| `Deve_aceitar_idProposta_Quando_formato_respeita_o_contrato` (`[Theory]`: 1 caractere, com `-`, com `_`, 64 caracteres) | `ValidadorPropostaRecebidaTests` | unitário | RN-07 |
| `Deve_rejeitar_idProposta_Quando_formato_e_invalido` (`[Theory]`: vazio, 65 caracteres, espaço, `#`, acento, espaço inicial) | `ValidadorPropostaRecebidaTests` | unitário | RN-07 |
| `Deve_rejeitar_documentoCpf_Quando_nao_tem_exatamente_onze_digitos` (`[Theory]`: vazio, 10, 12, com pontuação, com letra, com espaço, dígitos arábicos) | `ValidadorPropostaRecebidaTests` | unitário | RN-07, EC-19 |
| `Deve_aceitar_documentoCpf_Quando_digito_verificador_nao_confere` (`12345678901`, `00000000000`) | `ValidadorPropostaRecebidaTests` | unitário | RN-07, EC-19 |
| `Deve_aceitar_valores_Quando_positivos_e_com_ate_duas_casas` (inclui `25000.500m`) | `ValidadorPropostaRecebidaTests` | unitário | RN-05, RN-07, EC-13 |
| `Deve_rejeitar_valorSolicitado_Quando_nao_e_positivo` (`0m`, `-0.01m`, `-25000m`) | `ValidadorPropostaRecebidaTests` | unitário | RN-07 |
| `Deve_rejeitar_rendaMensal_Quando_nao_e_positiva` (mesmos valores) | `ValidadorPropostaRecebidaTests` | unitário | RN-07 |
| `Deve_rejeitar_valores_Quando_tem_mais_de_duas_casas_decimais` (`0.001m`, `25000.005m`, `8000.123m`) | `ValidadorPropostaRecebidaTests` | unitário | RN-05, EC-14 |
| `Deve_retornar_todos_os_erros_Quando_varios_campos_sao_invalidos` (ordem dos 4 campos) | `ValidadorPropostaRecebidaTests` | unitário | RN-07, RN-09 |
| `Deve_aceitar_Quando_proposta_aprovada_tem_limite_positivo` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_aceitar_Quando_proposta_reprovada_tem_limite_zero` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_aceitar_pontuacaoScore_Quando_esta_nos_limites` (`0`, `1000`) | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_rejeitar_pontuacaoScore_Quando_esta_fora_de_0_a_1000` (`-1`, `1001`, `int.MaxValue`) | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_rejeitar_limiteAprovado_Quando_negativo` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_rejeitar_limiteAprovado_Quando_aprovada_com_limite_zero` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_rejeitar_limiteAprovado_Quando_reprovada_com_limite_positivo` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_rejeitar_limiteAprovado_Quando_tem_mais_de_duas_casas_decimais` | `ValidadorPropostaAvaliadaTests` | unitário | RN-05, RN-08 |
| `Deve_rejeitar_avaliadoEm_Quando_fuso_nao_e_utc` | `ValidadorPropostaAvaliadaTests` | unitário | RN-06, RN-08, EC-17 |
| `Deve_rejeitar_enums_Quando_valor_e_o_padrao_zero` | `ValidadorPropostaAvaliadaTests` | unitário | RN-03, EC-11 |
| `Deve_rejeitar_idProposta_Quando_formato_e_invalido` | `ValidadorPropostaAvaliadaTests` | unitário | RN-08 |
| `Deve_omitir_cpf_Quando_ToString_de_PropostaRecebidaEvent_e_chamado` | `ProtecaoCpfTests` | unitário | RN-10 |
| `Deve_omitir_cpf_Quando_ToString_de_Proposta_e_chamado` | `ProtecaoCpfTests` | unitário | RN-10 |
| `Deve_nao_ter_campo_de_cpf_Quando_tipo_nao_precisa_do_documento` (`[Theory]`, 4 tipos) | `ProtecaoCpfTests` | unitário | RN-10 |
| `Deve_ser_record_selado_Quando_tipo_e_contrato_ou_entidade` (`[Theory]`, 6 tipos; record = tem o método `<Clone>$`) | `TiposDoDominioTests` | unitário | RN-01 |
| `Deve_ser_imutavel_Quando_tipo_e_contrato_ou_entidade` (`[Theory]`, 6 tipos; nenhum `set` público que não seja `init`) | `TiposDoDominioTests` | unitário | RN-01 |
| `Deve_nao_referenciar_nenhum_projeto_nem_pacote_Quando_projeto_e_Dominio` (já existe, spec 01) | `ReferenciasEntreProjetosTests` (Arquitetura) | arquitetura | RN-01, CA-05 |
| `Deve_referenciar_apenas_Dominio_Quando_projeto_e_Dominio_Tests` (novo) | `ReferenciasEntreProjetosTests` (Arquitetura) | arquitetura | RN-13 |
| `Deve_conter_todos_os_projetos_de_src_e_tests_Quando_solution_e_lida` (alterado para 12 / 6 / 6) | `SolutionTests` (Arquitetura) | arquitetura | RN-13, CA-06 |

### Verificações manuais

| ID | Comando | Resultado esperado | Cobre |
| --- | --- | --- | --- |
| VM-01 | `dotnet build` na raiz | `0 Warning(s)` e `0 Error(s)` | CA-01 |
| VM-02 | `dotnet test` na raiz | Todos os testes passam, incluindo os de `MotorCredito.Dominio.Tests` | CA-02 |
| VM-03 | `git diff main -- Directory.Packages.props src/MotorCredito.Dominio/MotorCredito.Dominio.csproj` | Nenhuma alteração | CA-05, CA-07 |

---

## 6. Critérios de Aceitação (CA)

- **CA-01** — `dotnet build` na raiz termina sem erros e sem warnings.
- **CA-02** — `dotnet test` na raiz passa, executando também `MotorCredito.Dominio.Tests`.
- **CA-03** — A mensagem do Cenário 1 do README, lida por `LeitorDeContratos.LerPropostaRecebida`, é válida e preenche os quatro campos com os valores do exemplo.
- **CA-04** — `PropostaAvaliadaEvent("PROP-2026-001", Aprovada, 750, Baixo, 25000m, 2026-11-09 10:00:01.200 UTC)` serializado com o `MotorCreditoJsonContext` gera exatamente o JSON da seção 2.
- **CA-05** — `MotorCredito.Dominio.csproj` continua sem `PackageReference` e sem `ProjectReference`.
- **CA-06** — A solution tem 12 projetos: 6 em `src/` e 6 em `tests/`.
- **CA-07** — `Directory.Packages.props` não muda.

---

## 7. Non-Goals

- Não implementa as regras de score, faixa e limite, nem a validação da entrada do motor: spec 06.
- Não define a regra de mascaramento do CPF (`***.456.789-**`) nem o formato dos logs: spec 05. Aqui o CPF só fica fora do `ToString()` e dos erros.
- Não cria o mapeamento BSON, os índices, os repositórios nem as transições de estado da `Proposta`: spec 04.
- Não cria a exceção base do projeto: nesta spec, conteúdo inválido vira `ResultadoLeitura`, não exceção.
- Não decodifica o `KafkaEvent` (base64, lote, chave) nem decide o que fazer com mensagens inválidas: specs 07 e 08.
- Não define o DTO de resposta HTTP da consulta: spec 09.
- Não cria variáveis de ambiente para tópicos e coleções: spec 03.
- Não adiciona versão de evento, envelope nem Schema Registry (fora do escopo no README).
- Não altera os handlers das Lambdas nem troca o serializador delas.
- Não cria `MotorCredito.Infraestrutura.Tests`.

---

## 8. Edge Cases

- **EC-01 — Payload é o literal `null`.** A desserialização devolve `null` sem exceção; o leitor devolve falha com o erro `("$", "payload vazio ou null")`.
- **EC-02 — Payload vazio, JSON cortado, array, texto solto ou não JSON.** O leitor devolve falha com um erro, sem lançar exceção.
- **EC-03 — Campo obrigatório ausente.** Falha na leitura.
- **EC-04 — Campo obrigatório com `null`.** Falha na leitura, com o erro em `$.<campo>`.
- **EC-05 — Nomes dos campos em PascalCase.** Os campos não são reconhecidos e contam como ausentes: falha.
- **EC-06 — Campo desconhecido.** Ignorado; a leitura é válida.
- **EC-07 — Campo repetido no mesmo objeto.** Falha, com o erro no campo repetido.
- **EC-08 — Enum com outra caixa, nome C#, número ou valor desconhecido.** Falha.
- **EC-09 — Enum com espaço no início ou no fim, ou com vírgula.** Falha. Sem o conversor estrito, `"EM_PROCESSAMENTO, APROVADA"` viraria `REPROVADA`.
- **EC-10 — `decisao` igual a `EM_PROCESSAMENTO`.** Falha: não é membro de `DecisaoCredito`.
- **EC-11 — Enum com valor `0` em objeto criado no código.** O validador devolve erro no campo; a serialização lança `JsonException`.
- **EC-12 — Valor monetário como texto (`"25000"`).** Falha.
- **EC-13 — Valor com escala maior mas só duas casas significativas (`25000.500`).** Aceito.
- **EC-14 — Valor com três casas significativas (`25000.005`).** Erro de validação no campo.
- **EC-15 — Número que não cabe em `decimal` (`1e30`).** Falha na leitura, com o erro no campo.
- **EC-16 — Data sem fuso.** Falha. O padrão do .NET interpretaria no fuso da máquina e daria resultados diferentes no notebook e na Lambda.
- **EC-17 — Data com fuso diferente de UTC.** No JSON, falha na leitura. Num objeto criado no código, o validador acusa erro em `avaliadoEm`, e a serialização converte para UTC.
- **EC-18 — Data com precisão abaixo de milissegundo.** É escrita truncada em milissegundos, então a ida e volta não devolve exatamente o mesmo valor. Quem carimba datas (specs 04 e 07) deve truncar para milissegundos, para o Kafka e o MongoDB guardarem o mesmo instante.
- **EC-19 — CPF.** Com pontuação, com letras, com outros dígitos Unicode ou com tamanho diferente de 11: erro. Com dígito verificador inválido (como o fictício `12345678901`): aceito, porque o dígito não é validado.
