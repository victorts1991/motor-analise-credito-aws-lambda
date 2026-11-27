using System.Text.Json.Serialization;
using MotorCredito.Dominio.Serializacao;

namespace MotorCredito.Dominio.Propostas;

[JsonConverter(typeof(EnumEstritoConverter<FaixaRisco>))]
public enum FaixaRisco
{
    [JsonStringEnumMemberName("BAIXO")]
    Baixo = 1,

    [JsonStringEnumMemberName("MEDIO")]
    Medio = 2,

    [JsonStringEnumMemberName("ALTO")]
    Alto = 3
}
