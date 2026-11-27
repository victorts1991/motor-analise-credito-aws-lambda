using MotorCredito.Dominio.Contratos;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Contratos;

public sealed class ValidadorPropostaAvaliadaTests
{
    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_aceitar_Quando_proposta_aprovada_tem_limite_positivo()
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta());

        erros.ShouldBeEmpty();
    }

    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_aceitar_Quando_proposta_reprovada_tem_limite_zero()
    {
        var erros = ValidadorPropostaAvaliada.Validar(
            CriarProposta(decisao: DecisaoCredito.Reprovada, pontuacaoScore: 0, faixaRisco: FaixaRisco.Alto, limiteAprovado: 0m));

        erros.ShouldBeEmpty();
    }

    // Spec 02 - cobre RN-08
    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Deve_aceitar_pontuacaoScore_Quando_esta_nos_limites(int pontuacao)
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(pontuacaoScore: pontuacao));

        erros.ShouldNotContain(erro => erro.Campo == "pontuacaoScore");
    }

    // Spec 02 - cobre RN-08
    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    [InlineData(int.MaxValue)]
    public void Deve_rejeitar_pontuacaoScore_Quando_esta_fora_de_0_a_1000(int pontuacao)
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(pontuacaoScore: pontuacao));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("pontuacaoScore");
    }

    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_rejeitar_limiteAprovado_Quando_negativo()
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(limiteAprovado: -0.01m));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("limiteAprovado");
    }

    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_rejeitar_limiteAprovado_Quando_aprovada_com_limite_zero()
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(limiteAprovado: 0m));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("limiteAprovado");
    }

    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_rejeitar_limiteAprovado_Quando_reprovada_com_limite_positivo()
    {
        var erros = ValidadorPropostaAvaliada.Validar(
            CriarProposta(decisao: DecisaoCredito.Reprovada, limiteAprovado: 0.01m));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("limiteAprovado");
    }

    // Spec 02 - cobre RN-05, RN-08
    [Fact]
    public void Deve_rejeitar_limiteAprovado_Quando_tem_mais_de_duas_casas_decimais()
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(limiteAprovado: 25000.005m));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("limiteAprovado");
    }

    // Spec 02 - cobre RN-06, RN-08, EC-17
    [Fact]
    public void Deve_rejeitar_avaliadoEm_Quando_fuso_nao_e_utc()
    {
        var data = new DateTimeOffset(2026, 11, 9, 10, 0, 1, TimeSpan.FromHours(-3));

        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(avaliadoEm: data));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("avaliadoEm");
    }

    // Spec 02 - cobre RN-03, EC-11
    [Fact]
    public void Deve_rejeitar_enums_Quando_valor_e_o_padrao_zero()
    {
        var proposta = CriarProposta(decisao: default, faixaRisco: default);

        var erros = ValidadorPropostaAvaliada.Validar(proposta);

        erros.Select(erro => erro.Campo).ShouldContain("decisao");
        erros.Select(erro => erro.Campo).ShouldContain("faixaRisco");
    }

    // Spec 02 - cobre RN-08
    [Fact]
    public void Deve_rejeitar_idProposta_Quando_formato_e_invalido()
    {
        var erros = ValidadorPropostaAvaliada.Validar(CriarProposta(idProposta: "ID # inválido"));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("idProposta");
    }

    private static PropostaAvaliadaEvent CriarProposta(
        string idProposta = "PROP-2026-001",
        DecisaoCredito decisao = DecisaoCredito.Aprovada,
        int pontuacaoScore = 750,
        FaixaRisco faixaRisco = FaixaRisco.Baixo,
        decimal limiteAprovado = 25000m,
        DateTimeOffset? avaliadoEm = null)
    {
        return new PropostaAvaliadaEvent(
            idProposta,
            decisao,
            pontuacaoScore,
            faixaRisco,
            limiteAprovado,
            avaliadoEm ?? new DateTimeOffset(2026, 11, 9, 10, 0, 1, TimeSpan.Zero));
    }
}
