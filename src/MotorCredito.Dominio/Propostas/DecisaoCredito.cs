using System.Text.Json.Serialization;
using MotorCredito.Dominio.Serializacao;

namespace MotorCredito.Dominio.Propostas;

[JsonConverter(typeof(EnumEstritoConverter<DecisaoCredito>))]
public enum DecisaoCredito
{
    [JsonStringEnumMemberName("APROVADA")]
    Aprovada = 1,

    [JsonStringEnumMemberName("REPROVADA")]
    Reprovada = 2
}
