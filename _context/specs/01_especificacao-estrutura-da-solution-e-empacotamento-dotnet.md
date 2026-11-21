# Spec 01 — Estrutura da Solution e Empacotamento .NET

- **Status:** Implementada
- **Objetivo:** criar a solution, os projetos .NET e as configurações compartilhadas, deixar as quatro Lambdas compilando com um handler esqueleto e gerar um pacote `.zip` por Lambda com um único script. As regras de arquitetura da spec 00 passam a ser verificadas por testes automatizados.
- **Depende de:** [Spec 00](./00_especificacao-constituicao-e-convencoes.md), [ADR 0002](../../docs/adr/0002-runtime-dotnet10-gerenciado-vs-native-aot.md)

---

## 1. Regras de Negócio (RN)

- **RN-01 — Solution única.** Existe uma única solution, `MotorAnaliseCredito.slnx`, na raiz do repositório, contendo todos os projetos de `src/` e `tests/`.
- **RN-02 — SDK fixado.** Um `global.json` na raiz fixa o SDK do .NET 10. Uma máquina com outros SDKs instalados (como o .NET 11) usa o SDK 10 dentro do repositório.
- **RN-03 — Propriedades comuns centralizadas.** `TargetFramework`, `Nullable`, `ImplicitUsings` e `TreatWarningsAsErrors` são definidos uma única vez, no `Directory.Build.props` da raiz. Nenhum `.csproj` redefine essas propriedades.
- **RN-04 — Versões de pacotes centralizadas.** As versões de todos os pacotes NuGet ficam no `Directory.Packages.props` da raiz (*Central Package Management*). Nenhum `.csproj` informa versão em `PackageReference`.
- **RN-05 — Projetos e referências.** Os projetos e as referências entre eles seguem exatamente a seção 3 da spec 00.
- **RN-06 — Lambdas com handler esqueleto.** Cada projeto de Lambda é uma biblioteca de classes com as propriedades de Lambda e uma classe `Function` com um handler esqueleto, que só devolve um texto identificando a função. Os handlers reais são criados nas specs 06 a 09.
- **RN-07 — Empacotamento por script único.** O script `scripts/empacotar-lambdas.sh` gera um `.zip` por Lambda em `dist/`, com o nome da função (`dist/motor-regras-score.zip`, por exemplo).
- **RN-08 — Arquitetura configurável.** A arquitetura dos pacotes vem da variável `LAMBDA_ARCHITECTURE` (`x86_64` ou `arm64`), com `x86_64` como padrão. A variável é documentada no `.env.example`.
- **RN-09 — Regras de arquitetura testadas.** As regras de referência entre projetos e a lista de pacotes proibidos da spec 00 são verificadas por testes automatizados no projeto `MotorCredito.Arquitetura.Tests`.

---

## 2. Comportamento Esperado

Do ponto de vista de quem clona o repositório:

1. Na raiz, `dotnet --version` mostra uma versão `10.0.x`, mesmo que a máquina tenha outros SDKs.
2. `dotnet build` compila os 11 projetos sem erros e sem warnings.
3. `dotnet test` executa os testes dos quatro projetos de Lambda e os testes de arquitetura, e todos passam.
4. `./scripts/empacotar-lambdas.sh`:
   1. carrega o `.env`, se existir;
   2. valida `LAMBDA_ARCHITECTURE` (usa `x86_64` se ausente);
   3. confere se o `dotnet` e o Amazon.Lambda.Tools estão instalados;
   4. cria a pasta `dist/`, se não existir;
   5. empacota as quatro Lambdas, uma por vez, mostrando o nome de cada uma;
   6. termina mostrando a lista dos `.zip` gerados.
5. Se qualquer etapa do script falhar, ele para na hora, mostra uma mensagem clara e termina com código de saída diferente de zero.

---

## 3. Arquivos e Onde Alterar

| Arquivo | Ação | O que muda |
| --- | --- | --- |
| `global.json` | criar | Fixa o SDK 10 |
| `Directory.Build.props` | criar | Propriedades comuns a todos os projetos |
| `Directory.Packages.props` | criar | Versões centralizadas dos pacotes |
| `MotorAnaliseCredito.slnx` | criar | Solution com os 11 projetos, separados nas pastas de solution `src` e `tests` |
| `.env.example` | criar | Documenta `LAMBDA_ARCHITECTURE` |
| `src/MotorCredito.Dominio/MotorCredito.Dominio.csproj` | criar | Biblioteca vazia, sem referências |
| `src/MotorCredito.Infraestrutura/MotorCredito.Infraestrutura.csproj` | criar | Biblioteca vazia, referencia `Dominio` |
| `src/MotorCredito.Lambda.MotorRegrasScore/` | criar | `.csproj`, `Function.cs`, `aws-lambda-tools-defaults.json` |
| `src/MotorCredito.Lambda.ProcessadorProposta/` | criar | `.csproj`, `Function.cs`, `aws-lambda-tools-defaults.json` |
| `src/MotorCredito.Lambda.DespachanteNotificacao/` | criar | `.csproj`, `Function.cs`, `aws-lambda-tools-defaults.json` |
| `src/MotorCredito.Lambda.ConsultaProposta/` | criar | `.csproj`, `Function.cs`, `aws-lambda-tools-defaults.json` |
| `tests/MotorCredito.Lambda.MotorRegrasScore.Tests/` | criar | Projeto de teste + `FunctionTests.cs` |
| `tests/MotorCredito.Lambda.ProcessadorProposta.Tests/` | criar | Projeto de teste + `FunctionTests.cs` |
| `tests/MotorCredito.Lambda.DespachanteNotificacao.Tests/` | criar | Projeto de teste + `FunctionTests.cs` |
| `tests/MotorCredito.Lambda.ConsultaProposta.Tests/` | criar | Projeto de teste + `FunctionTests.cs` |
| `tests/MotorCredito.Arquitetura.Tests/` | criar | Projeto de teste + testes de arquitetura |
| `scripts/empacotar-lambdas.sh` | criar | Script de empacotamento, com permissão de execução |
| `README.md` | alterar | Seção 10: nova linha na tabela para os testes de arquitetura |

Os projetos `MotorCredito.Dominio.Tests` e `MotorCredito.Infraestrutura.Tests` **não** são criados nesta spec: eles nascem nas specs que adicionarem lógica a esses projetos.

---

## 4. Implementação Detalhada

### 4.1 `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Com `latestFeature`, o .NET usa o SDK 10.0.x mais recente instalado e nunca passa para o 11.

### 4.2 `Directory.Build.props`

Define, para todos os projetos:

| Propriedade | Valor |
| --- | --- |
| `TargetFramework` | `net10.0` |
| `Nullable` | `enable` |
| `ImplicitUsings` | `enable` |
| `TreatWarningsAsErrors` | `true` |

### 4.3 `Directory.Packages.props`

- `ManagePackageVersionsCentrally` = `true`.
- Um `PackageVersion` para cada pacote usado nesta spec, com a **versão estável mais recente** disponível no NuGet no momento da implementação:

| Pacote | Usado por |
| --- | --- |
| `Amazon.Lambda.Core` | Projetos de Lambda |
| `Amazon.Lambda.Serialization.SystemTextJson` | Projetos de Lambda |
| `xunit` (v2, não o `xunit.v3`) | Projetos de teste |
| `xunit.runner.visualstudio` | Projetos de teste |
| `Microsoft.NET.Test.Sdk` | Projetos de teste |
| `NSubstitute` | Projetos de teste |
| `Shouldly` | Projetos de teste |
| `coverlet.collector` | Projetos de teste |

Os demais pacotes permitidos pela spec 00 entram nas specs que precisarem deles.

### 4.4 Projetos de Lambda

Cada projeto `MotorCredito.Lambda.<Nome>`:

- É uma biblioteca de classes (sem `OutputType` de executável).
- Tem no `.csproj`, além das referências:
  - `AWSProjectType` = `Lambda`
  - `GenerateRuntimeConfigurationFiles` = `true`
  - `CopyLocalLockFileAssemblies` = `true`
- Referencia os pacotes `Amazon.Lambda.Core` e `Amazon.Lambda.Serialization.SystemTextJson`.
- Referencia os projetos conforme a seção 3 da spec 00:
  - `MotorRegrasScore` → só `Dominio`;
  - as outras três → `Dominio` e `Infraestrutura`.
- Tem uma classe `sealed` `Function`, no namespace igual ao nome do projeto, com:
  - um construtor sem parâmetros;
  - o método `public string FunctionHandler(string entrada, ILambdaContext contexto)`, que devolve o texto `"<nome-da-funcao>: esqueleto"` (por exemplo, `"motor-regras-score: esqueleto"`).
- Registra o serializador com o atributo de assembly `LambdaSerializer`, usando o `DefaultLambdaJsonSerializer`. A troca para o serializador com *source generator* acontece nas specs 06 a 09, quando existirem os tipos reais de entrada e saída.
- Tem um `aws-lambda-tools-defaults.json` com `function-runtime` = `dotnet10`, `framework` = `net10.0`, `configuration` = `Release` e o `function-handler` da tabela abaixo.

| Função | Projeto | Handler |
| --- | --- | --- |
| `motor-regras-score` | `MotorCredito.Lambda.MotorRegrasScore` | `MotorCredito.Lambda.MotorRegrasScore::MotorCredito.Lambda.MotorRegrasScore.Function::FunctionHandler` |
| `processador-proposta` | `MotorCredito.Lambda.ProcessadorProposta` | `MotorCredito.Lambda.ProcessadorProposta::MotorCredito.Lambda.ProcessadorProposta.Function::FunctionHandler` |
| `despachante-notificacao` | `MotorCredito.Lambda.DespachanteNotificacao` | `MotorCredito.Lambda.DespachanteNotificacao::MotorCredito.Lambda.DespachanteNotificacao.Function::FunctionHandler` |
| `consulta-proposta` | `MotorCredito.Lambda.ConsultaProposta` | `MotorCredito.Lambda.ConsultaProposta::MotorCredito.Lambda.ConsultaProposta.Function::FunctionHandler` |

Os nomes de função e os handlers desta tabela são usados pela spec 03 para criar as Lambdas no LocalStack.

### 4.5 Projetos de teste das Lambdas

Cada `MotorCredito.Lambda.<Nome>.Tests`:

- Referencia o projeto da Lambda correspondente e os pacotes de teste da seção 4.3.
- Tem `IsTestProject` = `true`.
- Tem um arquivo `FunctionTests.cs` com o teste do handler esqueleto (seção 5). O `ILambdaContext` é um mock do NSubstitute.

### 4.6 Projeto `MotorCredito.Arquitetura.Tests`

- Não referencia nenhum projeto da solution. Referencia só os pacotes de teste.
- Todas as classes de teste têm `[Trait("Category", "Arquitetura")]`.
- Localiza a raiz do repositório subindo a partir de `AppContext.BaseDirectory` até achar o `MotorAnaliseCredito.slnx`. Se não achar, o teste falha com uma mensagem clara.
- Lê o `.slnx`, os `.csproj` e o `Directory.Packages.props` como XML (`System.Xml.Linq`).
- A lista de pacotes proibidos fica em **um único lugar** no projeto (uma classe estática), com os quatro pacotes da seção 4.2 da spec 00.
- Organização sugerida:

| Classe | Responsabilidade |
| --- | --- |
| `Repositorio` | Localizar a raiz e listar/carregar os arquivos de projeto |
| `PacotesProibidos` | Lista de pacotes proibidos |
| `SolutionTests` | Testes sobre o `.slnx` |
| `ReferenciasEntreProjetosTests` | Testes das regras de referência |
| `PacotesTests` | Testes de pacotes e versões |
| `PropriedadesComunsTests` | Testes de propriedades redefinidas nos `.csproj` |
| `LambdasTests` | Testes sobre os projetos de Lambda e o `aws-lambda-tools-defaults.json` |

### 4.7 `.env.example`

Contém só a variável desta spec, com um comentário explicando os valores aceitos:

```text
# Arquitetura dos pacotes das Lambdas: x86_64 (padrão) ou arm64
LAMBDA_ARCHITECTURE=x86_64
```

As demais variáveis entram na spec 03.

### 4.8 `scripts/empacotar-lambdas.sh`

- Começa com `#!/usr/bin/env bash` e `set -euo pipefail`.
- Funciona de qualquer pasta: calcula a raiz do repositório a partir da localização do próprio script.
- É compatível com o bash 3.2, que é o padrão do macOS: **não usa arrays associativos**.
- Carrega o `.env` da raiz, se existir, exportando as variáveis.
- Usa `LAMBDA_ARCHITECTURE`, com `x86_64` como padrão. Qualquer valor diferente de `x86_64` ou `arm64` faz o script parar com uma mensagem de erro.
- Confere se o `dotnet` e o `dotnet lambda` estão disponíveis. Se faltar um deles, para e mostra como instalar (`dotnet tool install -g Amazon.Lambda.Tools`).
- Define a lista de Lambdas **em um único lugar** do script, como pares `nome-da-funcao:pasta-do-projeto`.
- Para cada Lambda, executa `dotnet lambda package` com:
  - `--project-location` apontando para a pasta do projeto;
  - `--configuration Release`;
  - `--framework net10.0`;
  - `--function-architecture` com o valor de `LAMBDA_ARCHITECTURE`;
  - `--output-package dist/<nome-da-funcao>.zip`.
- Sobrescreve pacotes antigos com o mesmo nome.
- Ao final, lista os arquivos de `dist/`.
- O arquivo é salvo com permissão de execução (`chmod +x`).

### 4.9 README

Na tabela da seção 10, adicionar a linha:

| Camada | Escopo | Comando |
| --- | --- | --- |
| **Arquitetura** | Regras de referência entre projetos e pacotes proibidos | `dotnet test --filter "Category=Arquitetura"` |

---

## 5. Testes

### Testes automatizados

| Teste | Projeto | Tipo | Cobre |
| --- | --- | --- | --- |
| `Deve_conter_todos_os_projetos_de_src_e_tests_Quando_solution_e_lida` | Arquitetura | arquitetura | RN-01, RN-05, CA-04 |
| `Deve_nao_referenciar_nenhum_projeto_nem_pacote_Quando_projeto_e_Dominio` | Arquitetura | arquitetura | RN-05 |
| `Deve_referenciar_apenas_Dominio_Quando_projeto_e_Infraestrutura` | Arquitetura | arquitetura | RN-05 |
| `Deve_referenciar_apenas_Dominio_Quando_projeto_e_MotorRegrasScore` | Arquitetura | arquitetura | RN-05 |
| `Deve_nao_referenciar_outra_Lambda_Quando_projeto_e_Lambda` | Arquitetura | arquitetura | RN-05 |
| `Deve_nao_usar_pacote_proibido_Quando_qualquer_projeto_ou_props_e_lido` | Arquitetura | arquitetura | RN-09 |
| `Deve_nao_informar_versao_Quando_csproj_referencia_pacote` | Arquitetura | arquitetura | RN-04 |
| `Deve_ter_versao_central_Quando_pacote_e_referenciado` | Arquitetura | arquitetura | RN-04 |
| `Deve_nao_redefinir_propriedades_comuns_Quando_csproj_e_lido` | Arquitetura | arquitetura | RN-03 |
| `Deve_ter_propriedades_de_Lambda_Quando_projeto_e_Lambda` | Arquitetura | arquitetura | RN-06 |
| `Deve_ter_runtime_dotnet10_e_handler_esperado_Quando_defaults_da_Lambda_e_lido` | Arquitetura | arquitetura | RN-06 |
| `Deve_retornar_identificacao_Quando_handler_esqueleto_e_invocado` | Cada um dos 4 projetos de teste de Lambda | unitário | RN-06, CA-02 |

O teste `Deve_nao_referenciar_outra_Lambda_Quando_projeto_e_Lambda` e o `Deve_ter_propriedades_de_Lambda_Quando_projeto_e_Lambda` usam `[Theory]`, com um caso por projeto de Lambda.

### Verificações manuais

| ID | Comando | Resultado esperado | Cobre |
| --- | --- | --- | --- |
| VM-01 | `dotnet --version` na raiz | Versão `10.0.x` | RN-02, CA-03, EC-01 |
| VM-02 | `dotnet build` na raiz | `0 Warning(s)` e `0 Error(s)` | CA-01 |
| VM-03 | `dotnet test` na raiz | Todos os testes passam | CA-02 |
| VM-04 | `rm -rf dist && ./scripts/empacotar-lambdas.sh` sem `.env` | 4 arquivos `.zip` em `dist/`, um por função | RN-07, CA-05, EC-02, EC-04, EC-08 |
| VM-05 | `unzip -l dist/motor-regras-score.zip` | Lista `MotorCredito.Lambda.MotorRegrasScore.dll` e `MotorCredito.Lambda.MotorRegrasScore.runtimeconfig.json` | CA-06 |
| VM-06 | `LAMBDA_ARCHITECTURE=arm64 ./scripts/empacotar-lambdas.sh` | Termina sem erro e gera os 4 pacotes | RN-08, CA-07 |
| VM-07 | `LAMBDA_ARCHITECTURE=xyz ./scripts/empacotar-lambdas.sh; echo $?` | Mensagem de arquitetura inválida e código diferente de `0` | CA-08, EC-03 |
| VM-08 | Rodar o script duas vezes seguidas | A segunda execução termina sem erro e sobrescreve os pacotes | EC-05 |
| VM-09 | `cd scripts && ./empacotar-lambdas.sh` | Mesmo resultado da VM-04 | EC-06 |
| VM-10 | `PATH=/usr/bin:/bin ./scripts/empacotar-lambdas.sh; echo $?` | Mensagem dizendo que o `dotnet` não foi encontrado e código diferente de `0` | EC-07 |
| VM-11 | `cat .env.example` | Mostra `LAMBDA_ARCHITECTURE` com o comentário | RN-08 |
| VM-12 | Seção 10 do README | Tem a linha **Arquitetura** | CA-09 |

---

## 6. Critérios de Aceitação (CA)

- **CA-01** — `dotnet build` na raiz termina sem erros e sem warnings.
- **CA-02** — `dotnet test` na raiz passa, executando os testes dos 4 projetos de Lambda e do projeto de arquitetura.
- **CA-03** — `dotnet --version` na raiz mostra `10.0.x`.
- **CA-04** — A solution contém exatamente estes 11 projetos: os 6 de `src/` e os 5 de `tests/` listados na seção 3.
- **CA-05** — O script gera `dist/motor-regras-score.zip`, `dist/processador-proposta.zip`, `dist/despachante-notificacao.zip` e `dist/consulta-proposta.zip`.
- **CA-06** — Cada `.zip` contém a DLL e o `runtimeconfig.json` do projeto da Lambda.
- **CA-07** — Com `LAMBDA_ARCHITECTURE=arm64`, o script gera os pacotes sem erro.
- **CA-08** — Com um valor inválido em `LAMBDA_ARCHITECTURE`, o script para com mensagem de erro e código de saída diferente de zero.
- **CA-09** — A seção 10 do README tem a linha dos testes de arquitetura.

---

## 7. Non-Goals

- Não cria contratos, entidades, enums nem interfaces do domínio: spec 02.
- Não cria `docker-compose.yml`, scripts do LocalStack nem as demais variáveis do `.env.example`: spec 03.
- Não implementa os handlers reais nem troca o serializador pelo de *source generator*: specs 06 a 09.
- Não adiciona os pacotes `AWSSDK.Lambda`, `Confluent.Kafka`, `MongoDB.Driver`, `Amazon.Lambda.KafkaEvents`, `Amazon.Lambda.APIGatewayEvents` nem `Testcontainers.*`: entram nas specs que os usarem.
- Não cria `MotorCredito.Dominio.Tests` nem `MotorCredito.Infraestrutura.Tests`.
- Não aplica o padrão de dois construtores da spec 00 (seção 4.4): o handler esqueleto não tem dependências. O padrão passa a valer quando a Lambda tiver dependências.
- Não cria `.editorconfig`, analisadores extras nem configuração de cobertura mínima.

---

## 8. Edge Cases

- **EC-01 — Máquina com SDK 11 (ou outro) instalado.** O `global.json` faz o repositório usar o SDK 10.0.x mais recente instalado.
- **EC-02 — `LAMBDA_ARCHITECTURE` ausente.** O script usa `x86_64`.
- **EC-03 — `LAMBDA_ARCHITECTURE` com valor inválido.** O script para antes de empacotar, mostra os valores aceitos e termina com código diferente de zero.
- **EC-04 — Pasta `dist/` inexistente.** O script cria a pasta.
- **EC-05 — Script executado mais de uma vez.** Os pacotes são sobrescritos, sem erro.
- **EC-06 — Script executado de outra pasta.** O script funciona igual, porque resolve a raiz a partir da própria localização.
- **EC-07 — `dotnet` ou Amazon.Lambda.Tools ausente.** O script para e mostra como instalar o que falta.
- **EC-08 — `.env` ausente.** O script segue com os valores padrão.
