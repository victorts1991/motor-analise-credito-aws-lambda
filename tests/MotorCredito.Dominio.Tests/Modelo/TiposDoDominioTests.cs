using System.Reflection;
using System.Runtime.CompilerServices;
using MotorCredito.Dominio.Auditoria;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Score;
using Shouldly;

namespace MotorCredito.Dominio.Tests.Modelo;

public sealed class TiposDoDominioTests
{
    // Spec 02 - cobre RN-01
    [Theory]
    [MemberData(nameof(ContratosEEntidades))]
    public void Deve_ser_record_selado_Quando_tipo_e_contrato_ou_entidade(Type tipo)
    {
        tipo.IsSealed.ShouldBeTrue();
        tipo.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ShouldNotBeNull();
    }

    // Spec 02 - cobre RN-01
    [Theory]
    [MemberData(nameof(ContratosEEntidades))]
    public void Deve_ser_imutavel_Quando_tipo_e_contrato_ou_entidade(Type tipo)
    {
        var settersPublicos = tipo.GetProperties()
            .Select(propriedade => propriedade.SetMethod)
            .Where(setter => setter is { IsPublic: true })
            .OfType<MethodInfo>()
            .ToArray();

        settersPublicos.ShouldAllBe(setter =>
            setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)));
    }

    public static TheoryData<Type> ContratosEEntidades => new()
    {
        typeof(PropostaRecebidaEvent),
        typeof(PropostaAvaliadaEvent),
        typeof(AvaliacaoScoreRequest),
        typeof(AvaliacaoScoreResponse),
        typeof(Proposta),
        typeof(RegistroAuditoria)
    };
}
