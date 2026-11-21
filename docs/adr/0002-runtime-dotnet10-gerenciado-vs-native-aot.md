# ADR 0002 — Runtime gerenciado `dotnet10` em vez de Native AOT

- **Status:** Aceita
- **Data:** 2026-11-21

## Contexto

As Lambdas serão escritas em C#. Na AWS existem dois caminhos principais para .NET:

1. **Runtime gerenciado** (`dotnet10`): a AWS fornece e atualiza o runtime; publicamos só as DLLs da aplicação.
2. **Native AOT**: o código é compilado para binário nativo e roda no runtime `provided.al2023` (runtime customizado). O cold start costuma cair bastante, mas exige trimming, serialização JSON por *source generator* e bibliotecas compatíveis com AOT.

Pontos que pesam na decisão:

- O **.NET 8** chega ao fim de suporte no Lambda em **novembro de 2026**. O **.NET 10** é a versão LTS atual, com suporte no Lambda até **novembro de 2028**.
- O LocalStack suporta o runtime `dotnet10` desde a versão 4.13.
- Nem todas as bibliotecas do projeto têm compatibilidade garantida com trimming/AOT (`MongoDB.Driver` e `Confluent.Kafka` precisam ser validadas).
- Quem clonar o projeto pode estar em uma máquina `x86_64` ou `arm64` (como Macs com Apple Silicon).

## Decisão

- Usar o **runtime gerenciado `dotnet10`**, com `TargetFramework` `net10.0`.
- Fixar o SDK 10 no repositório com um `global.json` (a máquina pode ter outros SDKs instalados, como o 11).
- Usar **`x86_64`** como arquitetura padrão das Lambdas, configurável pela variável `LAMBDA_ARCHITECTURE` no `.env` (`x86_64` ou `arm64`). Quem estiver em uma máquina `arm64` troca o valor e evita que o LocalStack precise emular outra arquitetura.
- Serializar com `Amazon.Lambda.Serialization.SystemTextJson`. Sempre que possível, usar um `JsonSerializerContext` (*source generator*) desde o início, para não fechar a porta do AOT no futuro.

## Alternativas consideradas

| Alternativa | Por que não agora |
| --- | --- |
| **Native AOT** (`provided.al2023`) | Melhor cold start, mas aumenta muito a complexidade de build e exige validar a compatibilidade de cada biblioteca. O foco da POC é a arquitetura orientada a eventos, não a otimização de inicialização. |
| **.NET 8** gerenciado | Fim de suporte no Lambda em novembro de 2026. Começar um projeto novo nele não faz sentido. |
| **.NET 11** | Não é LTS e não tem runtime gerenciado no Lambda. |
| **Imagem de container** em vez de `.zip` | Útil para pacotes grandes ou dependências nativas, que não é o caso. O `.zip` é mais simples de carregar no LocalStack. |

## Consequências

**Positivas**

- Build e empacotamento simples com `dotnet lambda package`.
- Nenhuma restrição de reflection ou trimming nas bibliotecas.
- Runtime com suporte até 2028.

**Negativas**

- Cold start maior que o do AOT. Para uma POC local isso não é problema, mas precisa ser medido antes de produção.
- Na AWS real, `arm64` (Graviton) costuma sair mais barato que `x86_64`. A escolha de arquitetura deve ser revista no deploy.

## Quando revisitar

- Ao preparar um deploy real: medir cold start e avaliar Native AOT ou SnapStart, além de `arm64`.
- Quando a AWS anunciar o próximo runtime .NET LTS.