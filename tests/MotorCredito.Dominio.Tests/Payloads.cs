using System.Text;
using System.Text.Json.Nodes;

namespace MotorCredito.Dominio.Tests;

internal static class Payloads
{
    internal const string PropostaRecebida =
        """{"idProposta":"PROP-2026-001","documentoCpf":"12345678901","valorSolicitado":25000,"rendaMensal":8000}""";

    internal const string PropostaAvaliada =
        """{"idProposta":"PROP-2026-001","decisao":"APROVADA","pontuacaoScore":750,"faixaRisco":"BAIXO","limiteAprovado":25000,"avaliadoEm":"2026-11-09T10:00:01.200Z"}""";

    internal static byte[] EmUtf8(string json) => Encoding.UTF8.GetBytes(json);

    internal static string RemoverCampo(string json, string campo)
    {
        var objeto = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("O payload de teste precisa ser um objeto JSON.");
        objeto.Remove(campo);
        return objeto.ToJsonString();
    }

    internal static string DefinirCampoComoNull(string json, string campo)
    {
        var objeto = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("O payload de teste precisa ser um objeto JSON.");
        objeto[campo] = null;
        return objeto.ToJsonString();
    }

    internal static string AlterarCampo(string json, string campo, JsonNode? valor)
    {
        var objeto = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("O payload de teste precisa ser um objeto JSON.");
        objeto[campo] = valor;
        return objeto.ToJsonString();
    }

    internal static string AlterarCampo(string json, string campo, string valor)
    {
        var objeto = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("O payload de teste precisa ser um objeto JSON.");
        objeto[campo] = JsonValue.Create(valor);
        return objeto.ToJsonString();
    }
}
