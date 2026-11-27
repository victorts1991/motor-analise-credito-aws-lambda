using System.Text;

namespace MotorCredito.Dominio.Eventos;

public sealed record PropostaRecebidaEvent(
    string IdProposta,
    string DocumentoCpf,
    decimal ValorSolicitado,
    decimal RendaMensal)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(IdProposta)).Append(" = ").Append(IdProposta);
        builder.Append(", ").Append(nameof(DocumentoCpf)).Append(" = ***");
        builder.Append(", ").Append(nameof(ValorSolicitado)).Append(" = ").Append(ValorSolicitado);
        builder.Append(", ").Append(nameof(RendaMensal)).Append(" = ").Append(RendaMensal);
        return true;
    }
}
