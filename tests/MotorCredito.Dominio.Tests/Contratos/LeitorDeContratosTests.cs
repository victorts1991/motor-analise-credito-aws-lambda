using System.Text.Json;
using System.Text.Json.Nodes;
using MotorCredito.Dominio.Contratos;
using MotorCredito.Dominio.Eventos;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Contratos;

public sealed class LeitorDeContratosTests
{
    // Spec 02 - cobre RN-09, CA-03
    [Fact]
    public void Deve_ler_proposta_Quando_payload_e_o_exemplo_do_README()
    {
        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(Payloads.PropostaRecebida));

        resultado.Valido.ShouldBeTrue();
        resultado.Valor.ShouldBe(new PropostaRecebidaEvent("PROP-2026-001", "12345678901", 25000m, 8000m));
    }

    // Spec 02 - cobre RN-09
    [Fact]
    public void Deve_ler_proposta_avaliada_Quando_payload_respeita_o_contrato()
    {
        var resultado = LeitorDeContratos.LerPropostaAvaliada(Payloads.EmUtf8(Payloads.PropostaAvaliada));

        resultado.Valido.ShouldBeTrue();
        resultado.Valor.ShouldNotBeNull();
        resultado.Valor.IdProposta.ShouldBe("PROP-2026-001");
        resultado.Valor.LimiteAprovado.ShouldBe(25000m);
    }

    // Spec 02 - cobre RN-09, EC-01
    [Fact]
    public void Deve_falhar_sem_excecao_Quando_payload_e_o_literal_null()
    {
        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8("null"));

        resultado.Valido.ShouldBeFalse();
        resultado.Valor.ShouldBeNull();
        resultado.Erros.ShouldBe([new ErroValidacao("$", "payload vazio ou null")]);
    }

    // Spec 02 - cobre RN-09, EC-02
    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("{\"idProposta\":\"PROP-1\"")]
    [InlineData("[]")]
    [InlineData("\"texto\"")]
    [InlineData("isto nao e json")]
    public void Deve_falhar_sem_excecao_Quando_payload_nao_e_um_objeto_json_valido(string json)
    {
        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Valor.ShouldBeNull();
        resultado.Erros.Count.ShouldBe(1);
    }

    // Spec 02 - cobre RN-04, EC-03
    [Theory]
    [MemberData(nameof(CamposDaPropostaRecebida))]
    public void Deve_falhar_Quando_campo_obrigatorio_da_proposta_recebida_esta_ausente(string campo)
    {
        var json = Payloads.RemoverCampo(Payloads.PropostaRecebida, campo);

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Valor.ShouldBeNull();
    }

    // Spec 02 - cobre RN-04, EC-03
    [Theory]
    [MemberData(nameof(CamposDaPropostaAvaliada))]
    public void Deve_falhar_Quando_campo_obrigatorio_da_proposta_avaliada_esta_ausente(string campo)
    {
        var json = Payloads.RemoverCampo(Payloads.PropostaAvaliada, campo);

        var resultado = LeitorDeContratos.LerPropostaAvaliada(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Valor.ShouldBeNull();
    }

    // Spec 02 - cobre RN-04, EC-04
    [Theory]
    [MemberData(nameof(CamposDaPropostaRecebida))]
    public void Deve_falhar_Quando_campo_obrigatorio_vem_null(string campo)
    {
        var json = Payloads.DefinirCampoComoNull(Payloads.PropostaRecebida, campo);

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Erros.ShouldContain(erro => erro.Campo == $"$.{campo}");
    }

    // Spec 02 - cobre RN-02, EC-05
    [Fact]
    public void Deve_falhar_Quando_nomes_dos_campos_estao_em_PascalCase()
    {
        const string json =
            """{"IdProposta":"PROP-2026-001","DocumentoCpf":"12345678901","ValorSolicitado":25000,"RendaMensal":8000}""";

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Erros.Count.ShouldBe(1);
    }

    // Spec 02 - cobre RN-11, EC-06
    [Fact]
    public void Deve_ignorar_campo_desconhecido_Quando_payload_tem_campo_a_mais()
    {
        var json = Payloads.AlterarCampo(Payloads.PropostaRecebida, "novoCampoOpcional", JsonValue.Create("valor"));

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeTrue();
    }

    // Spec 02 - cobre RN-04, EC-07
    [Fact]
    public void Deve_falhar_Quando_campo_aparece_duas_vezes()
    {
        const string json =
            """{"idProposta":"PROP-2026-001","idProposta":"PROP-2026-002","documentoCpf":"12345678901","valorSolicitado":25000,"rendaMensal":8000}""";

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Valor.ShouldBeNull();
    }

    // Spec 02 - cobre RN-05, EC-12
    [Theory]
    [InlineData("valorSolicitado")]
    [InlineData("rendaMensal")]
    public void Deve_falhar_Quando_valor_monetario_vem_como_texto(string campo)
    {
        var json = Payloads.AlterarCampo(Payloads.PropostaRecebida, campo, JsonValue.Create("25000"));

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
    }

    // Spec 02 - cobre RN-05, EC-15
    [Fact]
    public void Deve_falhar_Quando_valor_monetario_excede_o_limite_do_decimal()
    {
        const string json =
            """{"idProposta":"PROP-2026-001","documentoCpf":"12345678901","valorSolicitado":1e30,"rendaMensal":8000}""";

        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Erros.Count.ShouldBe(1);
        resultado.Erros[0].Campo.ShouldBe("$.valorSolicitado");
    }

    // Spec 02 - cobre RN-08, RN-09
    [Fact]
    public void Deve_falhar_Quando_proposta_avaliada_reprovada_traz_limite()
    {
        var json = Payloads.AlterarCampo(Payloads.PropostaAvaliada, "decisao", "REPROVADA");

        var resultado = LeitorDeContratos.LerPropostaAvaliada(Payloads.EmUtf8(json));

        resultado.Valido.ShouldBeFalse();
        resultado.Erros.ShouldContain(erro => erro.Campo == "limiteAprovado");
    }

    // Spec 02 - cobre RN-09, RN-10
    [Theory]
    [InlineData("""{"idProposta":"PROP-2026-001","documentoCpf":"123.456.789-01","valorSolicitado":25000,"rendaMensal":8000}""")]
    [InlineData("""{"idProposta":"PROP-2026-001","documentoCpf":12345678901,"valorSolicitado":25000,"rendaMensal":8000}""")]
    [InlineData("""{"idProposta":"PROP-2026-001","documentoCpf":"12345678901","valorSolicitado":"25000","rendaMensal":8000}""")]
    [InlineData("""{"idProposta":"ID#INVÁLIDO","documentoCpf":"12345678901","valorSolicitado":25000,"rendaMensal":8000}""")]
    public void Deve_nao_expor_o_cpf_Quando_leitura_falha(string json)
    {
        var resultado = LeitorDeContratos.LerPropostaRecebida(Payloads.EmUtf8(json));
        var erros = string.Join('|', resultado.Erros.Select(erro => $"{erro.Campo}:{erro.Motivo}"));

        resultado.Valido.ShouldBeFalse();
        erros.ShouldNotContain("12345678901");
        erros.ShouldNotContain("456.789");
    }

    public static TheoryData<string> CamposDaPropostaRecebida => new()
    {
        "idProposta",
        "documentoCpf",
        "valorSolicitado",
        "rendaMensal"
    };

    public static TheoryData<string> CamposDaPropostaAvaliada => new()
    {
        "idProposta",
        "decisao",
        "pontuacaoScore",
        "faixaRisco",
        "limiteAprovado",
        "avaliadoEm"
    };
}
