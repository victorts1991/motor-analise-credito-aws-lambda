# Changelog

Histórico de mudanças do projeto, da mais recente para a mais antiga. Formato inspirado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/). Regras na spec 00, seção 4.8.

## 2026-11-21

### Adicionado

- Spec 01 implementada: solution `.slnx`, projetos de domínio, infraestrutura, quatro Lambdas e projetos de testes.
- Configuração `.NET 10`, pacotes centralizados e script de empacotamento das quatro Lambdas.
- Linha de testes de arquitetura na seção 10 do README.
- Spec 00 — constituição e convenções do projeto, incluindo o modelo de spec.
- `_context/memory.md` e `_context/changelog.md`.
- ADRs 0001 a 0004.
- Arquivos do Copilot em `.github/`: instruções globais, instruções para `src/` e `tests/` e o prompt `implementar-especificacao`.
- `.gitignore` padrão do .NET, ignorando também `.env` e `dist/`.

### Alterado

- README reescrito para C# / .NET 10.
- Specs movidas para `_context/specs/` e renumeradas de `00` a `10`.
- Specs 01 e 02 trocadas de ordem: a estrutura da solution vem antes dos contratos, porque os contratos são implementados dentro dos projetos que ela cria.
- Ciclo de vida da proposta no README: removido o estado `RECEBIDA`.
- Spec 00: verificações manuais (`VM-NN`) e testes de arquitetura (`Category=Arquitetura`) passam a fazer parte da rastreabilidade de testes.
- Instruções de testes do Copilot: regras para os testes de arquitetura.