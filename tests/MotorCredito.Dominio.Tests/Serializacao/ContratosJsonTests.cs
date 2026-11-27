using System.Text.Json;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Score;
using MotorCredito.Dominio.Serializacao;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Serializacao;

public sealed class ContratosJsonTests
{
    // Spec 02 - cobre RN-02, RN-12
    [Fact]
    public void Deve_gerar_json_em_camelCase_Quando_PropostaRecebidaEvent_e_serializado()
    {
        var evento = new PropostaRecebidaEvent("PROP-2026-001", "12345678901", 25000m, 8000m);

        var json = JsonSerializer.Serialize(evento, MotorCreditoJsonContext.Default.PropostaRecebidaEvent);

        json.ShouldBe(Payloads.PropostaRecebida);
    }

    // Spec 02 - cobre RN-02, RN-03, RN-06, RN-12, CA-04
    [Fact]
    public void Deve_gerar_json_do_contrato_Quando_PropostaAvaliadaEvent_e_serializado()
    {
        var evento = new PropostaAvaliadaEvent(
            "PROP-2026-001",
            DecisaoCredito.Aprovada,
            750,
            FaixaRisco.Baixo,
            25000m,
            new DateTimeOffset(2026, 11, 9, 10, 0, 1, 200, TimeSpan.Zero));

        var json = JsonSerializer.Serialize(evento, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);

        json.ShouldBe(Payloads.PropostaAvaliada);
    }

    // Spec 02 - cobre RN-02, RN-10, RN-12
    [Fact]
    public void Deve_gerar_json_do_contrato_Quando_AvaliacaoScoreRequest_e_serializado()
    {
        var request = new AvaliacaoScoreRequest("PROP-2026-001", 25000m, 8000m);

        var json = JsonSerializer.Serialize(request, MotorCreditoJsonContext.Default.AvaliacaoScoreRequest);

        json.ShouldBe("""{"idProposta":"PROP-2026-001","valorSolicitado":25000,"rendaMensal":8000}""");
    }

    // Spec 02 - cobre RN-02, RN-03, RN-12
    [Fact]
    public void Deve_gerar_json_do_contrato_Quando_AvaliacaoScoreResponse_e_serializado()
    {
        var response = new AvaliacaoScoreResponse(
            "PROP-2026-001",
            DecisaoCredito.Aprovada,
            750,
            FaixaRisco.Baixo,
            25000m);

        var json = JsonSerializer.Serialize(response, MotorCreditoJsonContext.Default.AvaliacaoScoreResponse);

        json.ShouldBe("""{"idProposta":"PROP-2026-001","decisao":"APROVADA","pontuacaoScore":750,"faixaRisco":"BAIXO","limiteAprovado":25000}""");
    }

    // Spec 02 - cobre RN-05, RN-12
    [Fact]
    public void Deve_preservar_valores_Quando_AvaliacaoScoreResponse_faz_ida_e_volta()
    {
        var original = new AvaliacaoScoreResponse(
            "PROP_2026-001",
            DecisaoCredito.Reprovada,
            0,
            FaixaRisco.Alto,
            0m);
        var json = JsonSerializer.Serialize(original, MotorCreditoJsonContext.Default.AvaliacaoScoreResponse);

        var resultado = JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.AvaliacaoScoreResponse);

        resultado.ShouldBe(original);
    }

    // Spec 02 - cobre RN-05, RN-06, RN-12
    [Fact]
    public void Deve_preservar_valores_Quando_PropostaAvaliadaEvent_faz_ida_e_volta()
    {
        var original = new PropostaAvaliadaEvent(
            "PROP-2026-001",
            DecisaoCredito.Aprovada,
            750,
            FaixaRisco.Baixo,
            25000.50m,
            new DateTimeOffset(2026, 11, 9, 10, 0, 1, 200, TimeSpan.Zero));
        var json = JsonSerializer.Serialize(original, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);

        var resultado = JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);

        resultado.ShouldBe(original);
    }

    // Spec 02 - cobre RN-12
    [Theory]
    [MemberData(nameof(TiposComMetadados))]
    public void Deve_ter_metadados_gerados_Quando_tipo_e_contrato(Type tipo)
    {
        var metadados = MotorCreditoJsonContext.Default.GetTypeInfo(tipo);

        metadados.ShouldNotBeNull();
    }

    public static TheoryData<Type> TiposComMetadados => new()
    {
        typeof(PropostaRecebidaEvent),
        typeof(PropostaAvaliadaEvent),
        typeof(AvaliacaoScoreRequest),
        typeof(AvaliacaoScoreResponse),
        typeof(StatusProposta),
        typeof(DecisaoCredito),
        typeof(FaixaRisco),
        typeof(MotorCredito.Dominio.Auditoria.TipoEventoAuditoria)
    };
}
