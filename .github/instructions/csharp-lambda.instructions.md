---
applyTo: "src/**/*.cs"
description: Regras para o código C# das Lambdas, do domínio e da infraestrutura.
---

# Regras para o código C# em `src/`

Estas regras complementam a [spec 00](../../_context/specs/00_especificacao-constituicao-e-convencoes.md). Em caso de conflito, a spec 00 prevalece.

## Estilo

- Use *file-scoped namespace* (`namespace MotorCredito.Dominio;`).
- Classes são `sealed` por padrão. Só deixe uma classe aberta para herança quando houver um motivo.
- Use `record` para contratos e DTOs (eventos, requests, responses).
- Respeite o *nullable*: não use o operador `!` para silenciar avisos. Trate o caso nulo.
- Comentários em português, e só para explicar **por que** algo é feito, nunca **o que** o código já diz.

## Projeto `MotorCredito.Dominio`

- Sem nenhuma referência a AWS SDK, MongoDB, Kafka, HTTP ou sistema de arquivos.
- Regras de negócio em métodos puros: mesma entrada, mesma saída, sem efeitos colaterais.
- Dependências externas (repositórios, clientes) existem aqui só como **interfaces**.

## Handlers das Lambdas (`MotorCredito.Lambda.*`)

- A classe `Function` tem dois construtores:
  - **sem parâmetros**: usado pelo runtime da Lambda; monta as dependências reais;
  - **com parâmetros**: recebe as dependências; usado pelos testes.
- O handler só orquestra: desserializa a entrada, chama o domínio ou os serviços e monta a saída. Regra de negócio não fica no handler.
- Clientes de infraestrutura (`MongoClient`, `IProducer`, `AmazonLambdaClient`) são criados **uma vez** e guardados em campo estático ou de instância, nunca dentro do método do handler.
- Variáveis de ambiente são lidas **uma vez**, na inicialização. Se uma variável obrigatória estiver ausente, lance uma exceção clara com o nome da variável, para a falha aparecer logo no *cold start*.
- Métodos com I/O são `async` e terminam em `Async`. Nunca use `.Result` ou `.Wait()`.
- O producer Kafka faz `Flush` antes de o handler retornar.

## Dados e tempo

- Valores monetários sempre em `decimal`.
- Datas sempre em UTC, obtidas de um `TimeProvider` recebido por injeção. Nunca use `DateTime.Now` nem `DateTime.UtcNow`.

## Erros e logs

- Use exceções de domínio específicas, herdando da exceção base do projeto.
- Nunca capture uma exceção sem registrar em log ou relançar.
- Nunca escreva o CPF completo em log, exceção ou resposta. Use a rotina de mascaramento da spec 05.
- Não use `Console.WriteLine` para log; use o mecanismo definido na spec 05.