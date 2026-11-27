namespace MotorCredito.Dominio.Score;

public sealed record AvaliacaoScoreRequest(
    string IdProposta,
    decimal ValorSolicitado,
    decimal RendaMensal);
