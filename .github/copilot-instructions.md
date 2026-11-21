# Instruções do Copilot para este repositório

Este repositório é uma POC de esteira de análise de crédito **serverless e orientada a eventos**: quatro AWS Lambdas em **C# (.NET 10)**, emuladas no **LocalStack**, que consomem e publicam eventos no **Apache Kafka** e persistem no **MongoDB**. Tudo roda localmente com `docker compose`.

O desenvolvimento é **orientado a especificações**: as specs em `_context/specs/` são a fonte da verdade.

## Antes de qualquer tarefa

Leia, nesta ordem:

1. `_context/memory.md`: estado atual do projeto, próximo passo e pendências.
2. `_context/specs/00_especificacao-constituicao-e-convencoes.md`: regras, convenções e modelo de spec. **Esta spec prevalece sobre qualquer outra instrução.**
3. A spec da tarefa e todos os arquivos listados em **Depende de** no cabeçalho dela.

## Regras que você deve seguir sempre

- Implemente **somente** o que a spec descreve. O que estiver em **Non-Goals** não é feito.
- Se a spec estiver ambígua, incompleta ou contradizer outra, **pare e pergunte**. Não invente regras de negócio, nomes de campos ou valores.
- Não comece uma spec se alguma spec da qual ela depende não estiver com status `Implementada`.
- Use apenas as bibliotecas permitidas na seção 4.2 da spec 00. Nunca use `FluentAssertions`, `Moq`, `Newtonsoft.Json` ou `Amazon.Lambda.Annotations`.
- O projeto `MotorCredito.Dominio` não pode referenciar AWS SDK, MongoDB, Kafka ou qualquer biblioteca de I/O.
- Nunca escreva o CPF completo em logs, mensagens de erro ou respostas HTTP.
- Nunca coloque endpoints, nomes de tópicos, nomes de funções ou credenciais fixos no código: use variáveis de ambiente e documente cada uma no `.env.example`.
- Nunca crie, altere ou leia o arquivo `.env`.
- Escreva os testes junto com o código e cite na seção **Testes** da spec qual teste cobre cada `RN`, `CA` e `EC`.
- Siga as convenções de nomenclatura da seção 4.3 da spec 00: termos do domínio em português, sufixos técnicos do .NET em inglês (`AvaliacaoScoreRequest`, `IPropostaRepository`).

## Ao terminar uma spec

Confira a **definição de pronto** da seção 2 da spec 00. Em especial:

1. `dotnet build` sem erros e sem warnings, e `dotnet test` passando.
2. Status da spec alterado para `Implementada`.
3. `_context/memory.md` **reescrito** com o novo estado (não acrescente histórico nele).
4. Nova entrada **no topo** do `_context/changelog.md`.
5. `README.md` atualizado, se algum comando ou passo de execução mudou.

## Decisões de arquitetura

As decisões vigentes estão em `docs/adr/`. Não contrarie uma ADR. Se uma tarefa exigir mudar uma decisão, pare e proponha uma nova ADR em vez de alterar o código.

## Idioma

Responda, escreva comentários, mensagens de commit e documentação em **português do Brasil**. Mensagens de commit seguem o padrão Conventional Commits, com uma linha (seção 4.6 da spec 00).

## Instruções complementares

- `.github/instructions/csharp-lambda.instructions.md`: regras para os arquivos C# das Lambdas.
- `.github/instructions/testes.instructions.md`: regras para os projetos de teste.
- `.github/prompts/implementar-especificacao.prompt.md`: prompt para implementar uma spec.