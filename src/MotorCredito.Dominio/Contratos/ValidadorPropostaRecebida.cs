using MotorCredito.Dominio.Eventos;

namespace MotorCredito.Dominio.Contratos;

public static class ValidadorPropostaRecebida
{
    public static IReadOnlyList<ErroValidacao> Validar(PropostaRecebidaEvent proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        var erros = new List<ErroValidacao>();
        if (!RegrasDeCampo.IdPropostaValido(proposta.IdProposta))
            erros.Add(new ErroValidacao("idProposta", "formato inválido"));

        if (!RegrasDeCampo.DocumentoCpfValido(proposta.DocumentoCpf))
            erros.Add(new ErroValidacao("documentoCpf", "deve conter exatamente 11 dígitos ASCII"));

        if (proposta.ValorSolicitado <= 0)
            erros.Add(new ErroValidacao("valorSolicitado", "deve ser maior que zero"));
        else if (!RegrasDeCampo.ValorMonetarioTemNoMaximoDuasCasas(proposta.ValorSolicitado))
            erros.Add(new ErroValidacao("valorSolicitado", "deve ter no máximo duas casas decimais"));

        if (proposta.RendaMensal <= 0)
            erros.Add(new ErroValidacao("rendaMensal", "deve ser maior que zero"));
        else if (!RegrasDeCampo.ValorMonetarioTemNoMaximoDuasCasas(proposta.RendaMensal))
            erros.Add(new ErroValidacao("rendaMensal", "deve ter no máximo duas casas decimais"));

        return erros;
    }
}
