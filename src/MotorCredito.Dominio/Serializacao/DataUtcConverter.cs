using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotorCredito.Dominio.Serializacao;

public sealed class DataUtcConverter : JsonConverter<DateTimeOffset>
{
    // Sem fuso explícito, o conversor padrão interpreta a data no fuso da máquina.
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();

        var texto = reader.GetString();
        if (texto is null
            || texto.Length <= 10
            || texto[10] != 'T'
            || !(texto.EndsWith('Z') || texto.EndsWith("+00:00", StringComparison.Ordinal))
            || !DateTimeOffset.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var data)
            || data.Offset != TimeSpan.Zero)
        {
            throw new JsonException();
        }

        return data;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        var utc = value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}
