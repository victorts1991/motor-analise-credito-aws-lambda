---
applyTo: "tests/**/*.cs"
description: Regras para os projetos de teste (xUnit, NSubstitute e Shouldly).
---

# Regras para os testes em `tests/`

Estas regras complementam a [spec 00](../../_context/specs/00_especificacao-constituicao-e-convencoes.md), seção 4.5. Em caso de conflito, a spec 00 prevalece.

## Ferramentas

- Framework: **xUnit**.
- Mocks: **NSubstitute** (`Substitute.For<T>()`). Nunca use Moq.
- Asserções: **Shouldly** (`resultado.ShouldBe(...)`). Nunca use FluentAssertions nem `Assert.*` do xUnit.
- Integração: **Testcontainers** para MongoDB e Kafka reais.

## Estrutura de cada teste

- Nome no formato `Deve_<resultado>_Quando_<condicao>`, em português.
- Corpo em **Arrange / Act / Assert**, com as três partes separadas por uma linha em branco.
- Um teste verifica **um** comportamento.
- Casos com a mesma lógica e dados diferentes usam `[Theory]` com `[InlineData]` ou `[MemberData]`.
- Acima de cada teste, um comentário indicando a spec e os itens cobertos:

```csharp
// Spec 06 — cobre RN-02, CA-01, EC-03
[Fact]
public void Deve_enquadrar_risco_alto_Quando_comprometimento_de_renda_passa_do_limite()
```

## Testes unitários

- Não acessam rede, banco, Kafka nem sistema de arquivos.
- Toda dependência externa é substituída por um mock do NSubstitute.
- O tempo é controlado com um `TimeProvider` falso: `Substitute.For<TimeProvider>()` com `GetUtcNow().Returns(...)`.
- Não usam `Thread.Sleep` nem `Task.Delay` para esperar algo acontecer.

## Testes de integração

- Marcados com `[Trait("Category", "Integracao")]`.
- Cada classe de teste sobe seus próprios containers (ou compartilha via *fixture* do xUnit) e usa um nome de banco único, para os testes não interferirem entre si.
- Não dependem do `docker compose` do projeto nem do LocalStack.

## Testes de arquitetura

- Marcados com `[Trait("Category", "Arquitetura")]`.
- Ficam no projeto `MotorCredito.Arquitetura.Tests`.
- São a única exceção à regra de não ler arquivos: leem o `.slnx`, os `.csproj` e os `.props` do repositório para verificar as regras de referência e os pacotes proibidos da spec 00.
- Localizam a raiz do repositório subindo a partir de `AppContext.BaseDirectory` até encontrar o `MotorAnaliseCredito.slnx`.

## Dados de teste

- Nunca use CPF real. Use valores claramente fictícios, como `12345678901`.
- Valores monetários em `decimal`, com o sufixo `m` (`25000m`).