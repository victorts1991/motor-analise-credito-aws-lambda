using System.Text;

namespace MotorCredito.Dominio.Propostas;

public sealed record Proposta
{
    public required string IdProposta { get; init; }
    public required string DocumentoCpf { get; init; }
    public required decimal ValorSolicitado { get; init; }
    public required decimal RendaMensal { get; init; }
    public required StatusProposta Status { get; init; }
    public required DateTimeOffset CriadoEm { get; init; }
    public int? PontuacaoScore { get; init; }
    public FaixaRisco? FaixaRisco { get; init; }
    public decimal? LimiteAprovado { get; init; }
    public DateTimeOffset? AvaliadoEm { get; init; }
    public DateTimeOffset? PublicadoEm { get; init; }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("IdProposta = ").Append(IdProposta);
        builder.Append(", DocumentoCpf = ***");
        builder.Append(", ValorSolicitado = ").Append(ValorSolicitado);
        builder.Append(", RendaMensal = ").Append(RendaMensal);
        builder.Append(", Status = ").Append(Status);
        builder.Append(", CriadoEm = ").Append(CriadoEm);
        builder.Append(", PontuacaoScore = ").Append(PontuacaoScore);
        builder.Append(", FaixaRisco = ").Append(FaixaRisco);
        builder.Append(", LimiteAprovado = ").Append(LimiteAprovado);
        builder.Append(", AvaliadoEm = ").Append(AvaliadoEm);
        builder.Append(", PublicadoEm = ").Append(PublicadoEm);
        return true;
    }
}
