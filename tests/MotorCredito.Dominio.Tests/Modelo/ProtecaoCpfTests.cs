using MotorCredito.Dominio.Auditoria;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Score;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Modelo;

public sealed class ProtecaoCpfTests
{
    // Spec 02 - cobre RN-10
    [Fact]
    public void Deve_omitir_cpf_Quando_ToString_de_PropostaRecebidaEvent_e_chamado()
    {
        var evento = new PropostaRecebidaEvent("PROP-1", "12345678901", 100m, 50m);

        var texto = evento.ToString();

        texto.ShouldContain("DocumentoCpf = ***");
        texto.ShouldNotContain("12345678901");
    }

    // Spec 02 - cobre RN-10
    [Fact]
    public void Deve_omitir_cpf_Quando_ToString_de_Proposta_e_chamado()
    {
        var proposta = new Proposta
        {
            IdProposta = "PROP-1",
            DocumentoCpf = "12345678901",
            ValorSolicitado = 100m,
            RendaMensal = 50m,
            Status = StatusProposta.EmProcessamento,
            CriadoEm = DateTimeOffset.UnixEpoch
        };

        var texto = proposta.ToString();

        texto.ShouldContain("DocumentoCpf = ***");
        texto.ShouldNotContain("12345678901");
    }

    // Spec 02 - cobre RN-10
    [Theory]
    [MemberData(nameof(TiposSemCpf))]
    public void Deve_nao_ter_campo_de_cpf_Quando_tipo_nao_precisa_do_documento(Type tipo)
    {
        tipo.GetProperties().ShouldNotContain(propriedade => propriedade.Name == "DocumentoCpf");
    }

    public static TheoryData<Type> TiposSemCpf => new()
    {
        typeof(AvaliacaoScoreRequest),
        typeof(AvaliacaoScoreResponse),
        typeof(PropostaAvaliadaEvent),
        typeof(RegistroAuditoria)
    };
}
