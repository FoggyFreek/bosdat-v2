using System.Text.Json;
using System.Text.Json.Serialization;

namespace BosDAT.API.Converters;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string TimeFormat = "HH:mm:ss";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("TimeOnly value cannot be null or empty");

        // Try parsing with seconds first (HH:mm:ss)
        if (TimeOnly.TryParseExact(value, TimeFormat, out var timeWithSeconds))
            return timeWithSeconds;

        // Fall back to HH:mm format
        if (TimeOnly.TryParseExact(value, "HH:mm", out var timeWithoutSeconds))
            return timeWithoutSeconds;

        throw new JsonException($"Unable to convert \"{value}\" to TimeOnly");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(TimeFormat));
    }
}
