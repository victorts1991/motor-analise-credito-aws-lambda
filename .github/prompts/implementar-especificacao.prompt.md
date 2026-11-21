---
name: implementar-especificacao
description: Implementa uma spec de _context/specs seguindo a spec 00 e a definição de pronto.
argument-hint: Número da spec, por exemplo 06
agent: agent
---

# Implementar a spec ${input:spec:número da spec, por exemplo 06}

Você vai implementar a spec de número **${input:spec}**, que está em `_context/specs/` com o nome começando por `${input:spec}_`.

Siga as etapas abaixo **na ordem**. Não pule etapas.

## Etapa 1 — Ler o contexto

Leia por completo:

1. [memory.md](../../_context/memory.md)
2. [Spec 00 — Constituição e convenções](../../_context/specs/00_especificacao-constituicao-e-convencoes.md)
3. A spec `${input:spec}`
4. Todos os arquivos listados em **Depende de** no cabeçalho da spec `${input:spec}`

## Etapa 2 — Verificar se pode começar

Confira os itens abaixo. Se **qualquer um** falhar, pare, explique o que falhou e não escreva código:

- [ ] A spec `${input:spec}` está com status `Aprovada`.
- [ ] Todas as specs listadas em **Depende de** estão com status `Implementada`.
- [ ] A branch atual se chama `spec/${input:spec}-<descricao-curta>`. Se não, peça para eu criar a branch; não crie você mesmo.
- [ ] A spec não tem ambiguidade, lacuna ou contradição com a spec 00 ou com as ADRs. Se tiver, liste cada ponto como pergunta.

## Etapa 3 — Apresentar o plano e aguardar

Antes de escrever código, mostre:

1. A lista de arquivos que serão criados ou alterados, baseada na seção **3. Arquivos e Onde Alterar**.
2. A lista de testes que serão escritos, baseada na seção **5. Testes**, com os `RN`, `CA` e `EC` que cada um cobre.
3. Qualquer `RN`, `CA` ou `EC` que ainda não tenha teste associado na spec.

**Aguarde minha confirmação** antes de seguir para a etapa 4.

## Etapa 4 — Implementar

- Implemente seguindo a seção **4. Implementação Detalhada** e as convenções da spec 00.
- Escreva os testes junto com o código.
- Não implemente nada que esteja em **7. Non-Goals**.
- Se surgir uma dúvida durante a implementação, pare e pergunte. Não invente regras, nomes de campos ou valores.
- Não altere outras specs nem ADRs. Se achar que uma precisa mudar, aponte no relatório final.

## Etapa 5 — Verificar

Rode e corrija até passar:

```bash
dotnet build
dotnet test
```

O build precisa terminar **sem erros e sem warnings**. Testes de integração (`Category=Integracao`) precisam do Docker rodando.

## Etapa 6 — Fechar a spec

1. Altere o status da spec `${input:spec}` para `Implementada`.
2. **Reescreva** o `_context/memory.md` com o novo estado: tabela de specs, próximo passo, armadilhas descobertas e pendências.
3. Adicione uma entrada **no topo** do `_context/changelog.md`, com a data de hoje.
4. Atualize o `README.md` se algum comando ou passo de execução mudou.

**Não faça commit nem push.** Eu faço os commits.

## Etapa 7 — Relatório final

Responda com:

1. **Rastreabilidade:** tabela com cada `RN`, `CA` e `EC` da spec e o teste que o cobre.
2. **Arquivos:** lista dos arquivos criados e alterados.
3. **Desvios:** qualquer ponto em que a implementação diferiu da spec, e por quê. Se não houver, escreva "Nenhum".
4. **Checklist** da definição de pronto (spec 00, seção 2), marcada.
5. **Sugestão de mensagem de commit** em uma linha, no padrão da seção 4.6 da spec 00.