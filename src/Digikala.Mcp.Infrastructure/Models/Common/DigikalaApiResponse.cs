using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Common;

public sealed class DigikalaApiResponse<T>
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
