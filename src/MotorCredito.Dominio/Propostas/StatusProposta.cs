using System.Text.Json.Serialization;
using MotorCredito.Dominio.Serializacao;

namespace MotorCredito.Dominio.Propostas;

[JsonConverter(typeof(EnumEstritoConverter<StatusProposta>))]
public enum StatusProposta
{
    [JsonStringEnumMemberName("EM_PROCESSAMENTO")]
    EmProcessamento = 1,

    [JsonStringEnumMemberName("APROVADA")]
    Aprovada = 2,

    [JsonStringEnumMemberName("REPROVADA")]
    Reprovada = 3
}
