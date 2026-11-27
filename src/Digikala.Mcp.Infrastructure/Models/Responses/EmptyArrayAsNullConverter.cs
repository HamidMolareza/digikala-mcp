using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

/// <summary>
/// Digikala's PHP backend serializes an empty object as <c>[]</c> (for example a Magnet product without a price),
/// so treat any array as null instead of failing the whole envelope.
/// </summary>
public sealed class EmptyArrayAsNullConverter<T> : JsonConverter<T?> where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
