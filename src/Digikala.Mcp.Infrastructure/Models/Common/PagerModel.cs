using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Common;

public sealed class PagerModel
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }
}
