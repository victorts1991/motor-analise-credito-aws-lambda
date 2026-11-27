using System.Text.Json.Serialization;
using MotorCredito.Dominio.Serializacao;

namespace MotorCredito.Dominio.Auditoria;

[JsonConverter(typeof(EnumEstritoConverter<TipoEventoAuditoria>))]
public enum TipoEventoAuditoria
{
    [JsonStringEnumMemberName("DECISAO_REGISTRADA")]
    DecisaoRegistrada = 1
}
