using System.Text.Json;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Serializacao;

namespace MotorCredito.Dominio.Contratos;

public static class LeitorDeContratos
{
    public static ResultadoLeitura<PropostaRecebidaEvent> LerPropostaRecebida(ReadOnlySpan<byte> utf8Json)
    {
        PropostaRecebidaEvent? proposta;
        try
        {
            proposta = JsonSerializer.Deserialize(utf8Json, MotorCreditoJsonContext.Default.PropostaRecebidaEvent);
        }
        catch (JsonException excecao)
        {
            return ResultadoLeitura<PropostaRecebidaEvent>.Falha(
                [new ErroValidacao(excecao.Path ?? "$", "payload inválido")]);
        }

        // O literal JSON null desserializa sem exceção; ainda assim não é um contrato válido.
        if (proposta is null)
        {
            return ResultadoLeitura<PropostaRecebidaEvent>.Falha(
                [new ErroValidacao("$", "payload vazio ou null")]);
        }

        var erros = ValidadorPropostaRecebida.Validar(proposta);
        return erros.Count == 0
            ? ResultadoLeitura<PropostaRecebidaEvent>.Sucesso(proposta)
            : ResultadoLeitura<PropostaRecebidaEvent>.Falha(erros);
    }

    public static ResultadoLeitura<PropostaAvaliadaEvent> LerPropostaAvaliada(ReadOnlySpan<byte> utf8Json)
    {
        PropostaAvaliadaEvent? proposta;
        try
        {
            proposta = JsonSerializer.Deserialize(utf8Json, MotorCreditoJsonContext.Default.PropostaAvaliadaEvent);
        }
        catch (JsonException excecao)
        {
            return ResultadoLeitura<PropostaAvaliadaEvent>.Falha(
                [new ErroValidacao(excecao.Path ?? "$", "payload inválido")]);
        }

        // O literal JSON null desserializa sem exceção; ainda assim não é um contrato válido.
        if (proposta is null)
        {
            return ResultadoLeitura<PropostaAvaliadaEvent>.Falha(
                [new ErroValidacao("$", "payload vazio ou null")]);
        }

        var erros = ValidadorPropostaAvaliada.Validar(proposta);
        return erros.Count == 0
            ? ResultadoLeitura<PropostaAvaliadaEvent>.Sucesso(proposta)
            : ResultadoLeitura<PropostaAvaliadaEvent>.Falha(erros);
    }
}
