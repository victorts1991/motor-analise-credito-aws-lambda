using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;

namespace MotorCredito.Dominio.Contratos;

public static class ValidadorPropostaAvaliada
{
    public static IReadOnlyList<ErroValidacao> Validar(PropostaAvaliadaEvent proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        var erros = new List<ErroValidacao>();
        if (!RegrasDeCampo.IdPropostaValido(proposta.IdProposta))
            erros.Add(new ErroValidacao("idProposta", "formato inválido"));

        if (!Enum.IsDefined(proposta.Decisao))
            erros.Add(new ErroValidacao("decisao", "valor inválido"));

        if (proposta.PontuacaoScore is < RegrasDeCampo.PontuacaoMinima or > RegrasDeCampo.PontuacaoMaxima)
            erros.Add(new ErroValidacao("pontuacaoScore", "deve estar entre 0 e 1000"));

        if (!Enum.IsDefined(proposta.FaixaRisco))
            erros.Add(new ErroValidacao("faixaRisco", "valor inválido"));

        if (proposta.LimiteAprovado < 0)
            erros.Add(new ErroValidacao("limiteAprovado", "não pode ser negativo"));
        else if (!RegrasDeCampo.ValorMonetarioTemNoMaximoDuasCasas(proposta.LimiteAprovado))
            erros.Add(new ErroValidacao("limiteAprovado", "deve ter no máximo duas casas decimais"));
        else if (proposta.Decisao == DecisaoCredito.Aprovada && proposta.LimiteAprovado == 0)
            erros.Add(new ErroValidacao("limiteAprovado", "deve ser maior que zero para decisão aprovada"));
        else if (proposta.Decisao == DecisaoCredito.Reprovada && proposta.LimiteAprovado != 0)
            erros.Add(new ErroValidacao("limiteAprovado", "deve ser zero para decisão reprovada"));

        if (proposta.AvaliadoEm.Offset != TimeSpan.Zero)
            erros.Add(new ErroValidacao("avaliadoEm", "deve estar em UTC"));

        return erros;
    }
}
