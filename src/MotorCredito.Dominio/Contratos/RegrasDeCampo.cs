namespace MotorCredito.Dominio.Contratos;

internal static class RegrasDeCampo
{
    internal const int TamanhoMaximoIdProposta = 64;
    internal const int TamanhoDocumentoCpf = 11;
    internal const int CasasDecimaisMaximas = 2;
    internal const int PontuacaoMinima = 0;
    internal const int PontuacaoMaxima = 1000;

    internal static bool IdPropostaValido(string valor)
    {
        return valor.Length is > 0 and <= TamanhoMaximoIdProposta
            && valor.All(caractere =>
                caractere is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '-'
                or '_');
    }

    internal static bool DocumentoCpfValido(string valor)
    {
        return valor.Length == TamanhoDocumentoCpf
            && valor.All(caractere => caractere is >= '0' and <= '9');
    }

    internal static bool ValorMonetarioTemNoMaximoDuasCasas(decimal valor)
    {
        return decimal.Round(valor, CasasDecimaisMaximas) == valor;
    }
}
