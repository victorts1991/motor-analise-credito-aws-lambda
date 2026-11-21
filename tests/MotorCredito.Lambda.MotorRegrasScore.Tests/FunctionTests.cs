using Amazon.Lambda.Core;
using NSubstitute;
using Shouldly;

namespace MotorCredito.Lambda.MotorRegrasScore.Tests;

public sealed class FunctionTests
{
    // Spec 01 - cobre RN-06, CA-02
    [Fact]
    public void Deve_retornar_identificacao_Quando_handler_esqueleto_e_invocado()
    {
        var function = new Function();
        var contexto = Substitute.For<ILambdaContext>();

        var resultado = function.FunctionHandler("entrada", contexto);

        resultado.ShouldBe("motor-regras-score: esqueleto");
    }
}
