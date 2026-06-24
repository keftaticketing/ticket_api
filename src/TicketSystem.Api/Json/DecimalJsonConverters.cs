namespace TicketSystem.Api.Json;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketSystem.Application.Common;

public sealed class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Money.Round(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteRawValue(Money.Round(value).ToString("F2", CultureInfo.InvariantCulture));
}

public sealed class NullableDecimalJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Money.Round(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteRawValue(Money.Round(value.Value).ToString("F2", CultureInfo.InvariantCulture));
    }
}
