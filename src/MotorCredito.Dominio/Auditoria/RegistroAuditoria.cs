using MotorCredito.Dominio.Propostas;

namespace MotorCredito.Dominio.Auditoria;

public sealed record RegistroAuditoria
{
    public required string IdProposta { get; init; }
    public required TipoEventoAuditoria Evento { get; init; }
    public required DecisaoCredito Decisao { get; init; }
    public required int PontuacaoScore { get; init; }
    public required FaixaRisco FaixaRisco { get; init; }
    public required decimal LimiteAprovado { get; init; }
    public required DateTimeOffset AvaliadoEm { get; init; }
    public required DateTimeOffset RegistradoEm { get; init; }
}
