using System.Text.Json;
using MotorCredito.Dominio.Auditoria;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Serializacao;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Serializacao;

public sealed class EnumsJsonTests
{
    // Spec 02 - cobre RN-03
    [Theory]
    [MemberData(nameof(ValoresDeEnum))]
    public void Deve_usar_o_texto_do_contrato_Quando_enum_e_serializado(object valor, string esperado)
    {
        var json = SerializarEnum(valor);

        json.ShouldBe($"\"{esperado}\"");
        DesserializarEnum(json, valor.GetType()).ShouldBe(valor);
    }

    // Spec 02 - cobre RN-03, EC-08, EC-10
    [Theory]
    [InlineData("\"aprovada\"")]
    [InlineData("\"Aprovada\"")]
    [InlineData("\"APROVADO\"")]
    [InlineData("\"EM_PROCESSAMENTO\"")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("true")]
    [InlineData("{}")]
    public void Deve_rejeitar_decisao_Quando_texto_nao_e_exatamente_o_do_contrato(string json)
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.DecisaoCredito));
    }

    // Spec 02 - cobre RN-03, EC-08
    [Theory]
    [InlineData("\"baixo\"")]
    [InlineData("\"Medio\"")]
    [InlineData("\"MÉDIO\"")]
    [InlineData("\"CRITICO\"")]
    [InlineData("2")]
    public void Deve_rejeitar_faixaRisco_Quando_texto_nao_e_exatamente_o_do_contrato(string json)
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.FaixaRisco));
    }

    // Spec 02 - cobre RN-03, EC-09
    [Theory]
    [InlineData("\" APROVADA\"")]
    [InlineData("\"APROVADA \"")]
    [InlineData("\"EM_PROCESSAMENTO, APROVADA\"")]
    [InlineData("\"APROVADA,REPROVADA\"")]
    [InlineData("\"APROVADA, APROVADA\"")]
    public void Deve_rejeitar_enum_Quando_texto_tem_espaco_ou_virgula(string json)
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.StatusProposta));
    }

    // Spec 02 - cobre RN-03, EC-11
    [Fact]
    public void Deve_lancar_excecao_Quando_evento_com_enum_zero_e_serializado()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Serialize(
                new MotorCredito.Dominio.Eventos.PropostaAvaliadaEvent(
                    "PROP-2026-001",
                    default,
                    750,
                    FaixaRisco.Baixo,
                    25000m,
                    DateTimeOffset.UnixEpoch),
                MotorCreditoJsonContext.Default.PropostaAvaliadaEvent));
    }

    // Spec 02 - cobre RN-03
    [Theory]
    [MemberData(nameof(EnumsSemMembroZero))]
    public void Deve_nao_ter_membro_zero_Quando_enum_e_do_contrato(Type tipo)
    {
        Enum.GetValues(tipo).Cast<object>().ShouldNotContain(valor => Convert.ToInt32(valor) == 0);
    }

    public static TheoryData<object, string> ValoresDeEnum => new()
    {
        { StatusProposta.EmProcessamento, "EM_PROCESSAMENTO" },
        { StatusProposta.Aprovada, "APROVADA" },
        { StatusProposta.Reprovada, "REPROVADA" },
        { DecisaoCredito.Aprovada, "APROVADA" },
        { DecisaoCredito.Reprovada, "REPROVADA" },
        { FaixaRisco.Baixo, "BAIXO" },
        { FaixaRisco.Medio, "MEDIO" },
        { FaixaRisco.Alto, "ALTO" },
        { TipoEventoAuditoria.DecisaoRegistrada, "DECISAO_REGISTRADA" }
    };

    public static TheoryData<Type> EnumsSemMembroZero => new()
    {
        typeof(StatusProposta),
        typeof(DecisaoCredito),
        typeof(FaixaRisco),
        typeof(TipoEventoAuditoria)
    };

    private static string SerializarEnum(object valor)
    {
        return valor switch
        {
            StatusProposta status => JsonSerializer.Serialize(status, MotorCreditoJsonContext.Default.StatusProposta),
            DecisaoCredito decisao => JsonSerializer.Serialize(decisao, MotorCreditoJsonContext.Default.DecisaoCredito),
            FaixaRisco faixa => JsonSerializer.Serialize(faixa, MotorCreditoJsonContext.Default.FaixaRisco),
            TipoEventoAuditoria evento => JsonSerializer.Serialize(evento, MotorCreditoJsonContext.Default.TipoEventoAuditoria),
            _ => throw new ArgumentOutOfRangeException(nameof(valor))
        };
    }

    private static object? DesserializarEnum(string json, Type tipo)
    {
        if (tipo == typeof(StatusProposta))
            return JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.StatusProposta);
        if (tipo == typeof(DecisaoCredito))
            return JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.DecisaoCredito);
        if (tipo == typeof(FaixaRisco))
            return JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.FaixaRisco);
        if (tipo == typeof(TipoEventoAuditoria))
            return JsonSerializer.Deserialize(json, MotorCreditoJsonContext.Default.TipoEventoAuditoria);

        throw new ArgumentOutOfRangeException(nameof(tipo));
    }
}
