using System.Text.Json;
using System.Text.Json.Nodes;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Serializacao;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Serializacao;

public sealed class DatasJsonTests
{
    // Spec 02 - cobre RN-06, EC-17, EC-18
    [Theory]
    [MemberData(nameof(DatasParaSerializar))]
    public void Deve_escrever_em_utc_com_milissegundos_Quando_data_e_serializada(
        DateTimeOffset data,
        string textoEsperado)
    {
        var evento = CriarEvento(data);

        var json = JsonSerializer.Serialize(evento, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);

        JsonDocument.Parse(json).RootElement.GetProperty("avaliadoEm").GetString().ShouldBe(textoEsperado);
    }

    // Spec 02 - cobre RN-06
    [Theory]
    [InlineData("2026-11-09T10:00:01.200Z")]
    [InlineData("2026-11-09T10:00:01Z")]
    [InlineData("2026-11-09T10:00:01.200+00:00")]
    public void Deve_aceitar_data_Quando_fuso_utc_e_explicito(string data)
    {
        var json = Payloads.AlterarCampo(Payloads.PropostaAvaliada, "avaliadoEm", data);

        var resultado = JsonSerializer.Deserialize(
            json,
            MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);

        resultado.ShouldNotBeNull();
        resultado.AvaliadoEm.Offset.ShouldBe(TimeSpan.Zero);
    }

    // Spec 02 - cobre RN-06, EC-16, EC-17
    [Theory]
    [InlineData("\"2026-11-09T10:00:01.200\"")]
    [InlineData("\"2026-11-09T10:00:01.200-03:00\"")]
    [InlineData("\"2026-11-09\"")]
    [InlineData("\"2026-02-30T10:00:01Z\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    public void Deve_rejeitar_data_Quando_fuso_utc_nao_e_explicito_ou_formato_e_invalido(string jsonData)
    {
        var payload = Payloads.AlterarCampo(
            Payloads.PropostaAvaliada,
            "avaliadoEm",
            JsonNode.Parse(jsonData));

        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize(payload, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent));
    }

    public static TheoryData<DateTimeOffset, string> DatasParaSerializar => new()
    {
        {
            new DateTimeOffset(2026, 11, 9, 10, 0, 1, 200, TimeSpan.Zero),
            "2026-11-09T10:00:01.200Z"
        },
        {
            new DateTimeOffset(2026, 11, 9, 10, 0, 1, TimeSpan.Zero),
            "2026-11-09T10:00:01.000Z"
        },
        {
            new DateTimeOffset(2026, 11, 9, 7, 0, 1, 200, TimeSpan.FromHours(-3)),
            "2026-11-09T10:00:01.200Z"
        },
        {
            new DateTimeOffset(2026, 11, 9, 10, 0, 1, 200, TimeSpan.Zero).AddTicks(9999),
            "2026-11-09T10:00:01.200Z"
        }
    };

    private static PropostaAvaliadaEvent CriarEvento(DateTimeOffset data)
    {
        return new PropostaAvaliadaEvent(
            "PROP-2026-001",
            DecisaoCredito.Aprovada,
            750,
            FaixaRisco.Baixo,
            25000m,
            data);
    }
}
