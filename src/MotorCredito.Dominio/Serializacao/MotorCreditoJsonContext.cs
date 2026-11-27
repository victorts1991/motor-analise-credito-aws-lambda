using System.Text.Json;
using System.Text.Json.Serialization;
using MotorCredito.Dominio.Auditoria;
using MotorCredito.Dominio.Eventos;
using MotorCredito.Dominio.Propostas;
using MotorCredito.Dominio.Score;

namespace MotorCredito.Dominio.Serializacao;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true,
    AllowDuplicateProperties = false,
    NumberHandling = JsonNumberHandling.Strict,
    Converters = [typeof(DataUtcConverter)])]
[JsonSerializable(typeof(PropostaRecebidaEvent))]
[JsonSerializable(typeof(PropostaAvaliadaEvent))]
[JsonSerializable(typeof(AvaliacaoScoreRequest))]
[JsonSerializable(typeof(AvaliacaoScoreResponse))]
[JsonSerializable(typeof(StatusProposta))]
[JsonSerializable(typeof(DecisaoCredito))]
[JsonSerializable(typeof(FaixaRisco))]
[JsonSerializable(typeof(TipoEventoAuditoria))]
public sealed partial class MotorCreditoJsonContext : JsonSerializerContext;
