namespace MotorCredito.Dominio.Contratos;

public sealed record ResultadoLeitura<T> where T : class
{
    private ResultadoLeitura(T? valor, IReadOnlyList<ErroValidacao> erros)
    {
        Valor = valor;
        Erros = erros;
    }

    public T? Valor { get; }
    public IReadOnlyList<ErroValidacao> Erros { get; }
    public bool Valido => Valor is not null && Erros.Count == 0;

    internal static ResultadoLeitura<T> Sucesso(T valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new ResultadoLeitura<T>(valor, []);
    }

    internal static ResultadoLeitura<T> Falha(IReadOnlyList<ErroValidacao> erros)
    {
        ArgumentNullException.ThrowIfNull(erros);
        return new ResultadoLeitura<T>(null, erros);
    }
}
