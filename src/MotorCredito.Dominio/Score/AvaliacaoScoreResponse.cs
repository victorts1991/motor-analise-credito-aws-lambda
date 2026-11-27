using MotorCredito.Dominio.Propostas;

namespace MotorCredito.Dominio.Score;

public sealed record AvaliacaoScoreResponse(
    string IdProposta,
    DecisaoCredito Decisao,
    int PontuacaoScore,
    FaixaRisco FaixaRisco,
    decimal LimiteAprovado);
