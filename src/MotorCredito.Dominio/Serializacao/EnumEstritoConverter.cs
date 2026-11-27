using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotorCredito.Dominio.Serializacao;

public sealed class EnumEstritoConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    // O conversor padrão tolera espaços e vírgulas; "EM_PROCESSAMENTO, APROVADA" vira REPROVADA.
    private static readonly IReadOnlyDictionary<string, TEnum> ValoresPorTexto = CriarValoresPorTexto();
    private static readonly IReadOnlyDictionary<TEnum, string> TextosPorValor =
        ValoresPorTexto.ToDictionary(par => par.Value, par => par.Key);

    public override TEnum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();

        var texto = reader.GetString();
        if (texto is null
            || texto.Contains(',', StringComparison.Ordinal)
            || !string.Equals(texto, texto.Trim(), StringComparison.Ordinal)
            || !ValoresPorTexto.TryGetValue(texto, out var valor))
        {
            throw new JsonException();
        }

        return valor;
    }

    public override void Write(
        Utf8JsonWriter writer,
        TEnum value,
        JsonSerializerOptions options)
    {
        if (!TextosPorValor.TryGetValue(value, out var texto))
            throw new JsonException();

        writer.WriteStringValue(texto);
    }

    private static IReadOnlyDictionary<string, TEnum> CriarValoresPorTexto()
    {
        var valores = new Dictionary<string, TEnum>(StringComparer.Ordinal);

        foreach (var valor in Enum.GetValues<TEnum>())
        {
            var nome = Enum.GetName(valor);
            if (nome is null)
                throw new InvalidOperationException($"Não foi possível obter o nome do enum {typeof(TEnum).Name}.");

            var atributo = typeof(TEnum)
                .GetField(nome, BindingFlags.Public | BindingFlags.Static)?
                .GetCustomAttribute<JsonStringEnumMemberNameAttribute>();

            if (atributo is null || string.IsNullOrEmpty(atributo.Name))
                throw new InvalidOperationException($"O membro {nome} não possui nome JSON configurado.");

            valores.Add(atributo.Name, valor);
        }

        return valores;
    }
}
