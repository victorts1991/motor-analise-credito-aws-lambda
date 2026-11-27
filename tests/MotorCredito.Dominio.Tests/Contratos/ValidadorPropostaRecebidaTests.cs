using MotorCredito.Dominio.Contratos;
using MotorCredito.Dominio.Eventos;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Contratos;

public sealed class ValidadorPropostaRecebidaTests
{
    // Spec 02 - cobre RN-07
    [Theory]
    [InlineData("A")]
    [InlineData("PROP-1")]
    [InlineData("PROP_1")]
    [MemberData(nameof(IdPropostaComTamanhoMaximo))]
    public void Deve_aceitar_idProposta_Quando_formato_respeita_o_contrato(string idProposta)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(idProposta: idProposta));

        erros.ShouldBeEmpty();
    }

    // Spec 02 - cobre RN-07
    [Theory]
    [InlineData("")]
    [MemberData(nameof(IdPropostaLongo))]
    [InlineData("PROP 1")]
    [InlineData("PROP#1")]
    [InlineData("PRÓP-1")]
    [InlineData(" PROP-1")]
    public void Deve_rejeitar_idProposta_Quando_formato_e_invalido(string idProposta)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(idProposta: idProposta));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("idProposta");
    }

    // Spec 02 - cobre RN-07, EC-19
    [Theory]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("123.456.789-01")]
    [InlineData("1234567890a")]
    [InlineData("1234567890 ")]
    [InlineData("١٢٣٤٥٦٧٨٩٠١")]
    public void Deve_rejeitar_documentoCpf_Quando_nao_tem_exatamente_onze_digitos(string cpf)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(cpf: cpf));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("documentoCpf");
    }

    // Spec 02 - cobre RN-07, EC-19
    [Theory]
    [InlineData("12345678901")]
    [InlineData("00000000000")]
    public void Deve_aceitar_documentoCpf_Quando_digito_verificador_nao_confere(string cpf)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(cpf: cpf));

        erros.ShouldNotContain(erro => erro.Campo == "documentoCpf");
    }

    // Spec 02 - cobre RN-05, RN-07, EC-13
    [Theory]
    [MemberData(nameof(ValoresMonetariosValidos))]
    public void Deve_aceitar_valores_Quando_positivos_e_com_ate_duas_casas(decimal valor)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(valorSolicitado: valor, rendaMensal: valor));

        erros.ShouldBeEmpty();
    }

    // Spec 02 - cobre RN-07
    [Theory]
    [MemberData(nameof(ValoresNaoPositivos))]
    public void Deve_rejeitar_valorSolicitado_Quando_nao_e_positivo(decimal valor)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(valorSolicitado: valor));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("valorSolicitado");
    }

    // Spec 02 - cobre RN-07
    [Theory]
    [MemberData(nameof(ValoresNaoPositivos))]
    public void Deve_rejeitar_rendaMensal_Quando_nao_e_positiva(decimal valor)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(rendaMensal: valor));

        erros.ShouldHaveSingleItem().Campo.ShouldBe("rendaMensal");
    }

    // Spec 02 - cobre RN-05, EC-14
    [Theory]
    [MemberData(nameof(ValoresComMaisDeDuasCasas))]
    public void Deve_rejeitar_valores_Quando_tem_mais_de_duas_casas_decimais(decimal valor)
    {
        var erros = ValidadorPropostaRecebida.Validar(CriarProposta(valorSolicitado: valor, rendaMensal: valor));

        erros.Select(erro => erro.Campo).ShouldBe(["valorSolicitado", "rendaMensal"]);
    }

    // Spec 02 - cobre RN-07, RN-09
    [Fact]
    public void Deve_retornar_todos_os_erros_Quando_varios_campos_sao_invalidos()
    {
        var proposta = new PropostaRecebidaEvent("ID INVÁLIDO", "123", 0m, -1m);

        var erros = ValidadorPropostaRecebida.Validar(proposta);

        erros.Select(erro => erro.Campo)
            .ShouldBe(["idProposta", "documentoCpf", "valorSolicitado", "rendaMensal"]);
    }

    public static TheoryData<string> IdPropostaComTamanhoMaximo => new()
    {
        new string('A', 64)
    };

    public static TheoryData<string> IdPropostaLongo => new()
    {
        new string('A', 65)
    };

    public static TheoryData<decimal> ValoresMonetariosValidos => new()
    {
        0.01m,
        25000m,
        25000.50m,
        25000.500m
    };

    public static TheoryData<decimal> ValoresNaoPositivos => new()
    {
        0m,
        -0.01m,
        -25000m
    };

    public static TheoryData<decimal> ValoresComMaisDeDuasCasas => new()
    {
        0.001m,
        25000.005m,
        8000.123m
    };

    private static PropostaRecebidaEvent CriarProposta(
        string idProposta = "PROP-2026-001",
        string cpf = "12345678901",
        decimal valorSolicitado = 25000m,
        decimal rendaMensal = 8000m)
    {
        return new PropostaRecebidaEvent(idProposta, cpf, valorSolicitado, rendaMensal);
    }
}
