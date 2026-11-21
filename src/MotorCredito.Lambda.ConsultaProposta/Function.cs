using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace MotorCredito.Lambda.ConsultaProposta;

public sealed class Function
{
    public Function()
    {
    }

    public string FunctionHandler(string entrada, ILambdaContext contexto)
    {
        _ = entrada;
        _ = contexto;

        return "consulta-proposta: esqueleto";
    }
}
