using MotorCredito.Dominio.Propostas;

namespace MotorCredito.Dominio.Eventos;

public sealed record PropostaAvaliadaEvent(
    string IdProposta,
    DecisaoCredito Decisao,
    int PontuacaoScore,
    FaixaRisco FaixaRisco,
    decimal LimiteAprovado,
    DateTimeOffset AvaliadoEm);
