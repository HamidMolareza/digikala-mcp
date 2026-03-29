using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class AutocompleteData
{
    [JsonPropertyName("trends")]
    public List<AutocompleteTrendItem>? Trends { get; set; }

    [JsonPropertyName("trending_version")]
    public string? TrendingVersion { get; set; }

    [JsonPropertyName("banner")]
    public object? Banner { get; set; }

    [JsonPropertyName("suggestions")]
    public List<AutocompleteSuggestionItem>? Suggestions { get; set; }
}

public sealed class AutocompleteTrendItem
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}

public sealed class AutocompleteSuggestionItem
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("category")]
    public CategoryRef? Category { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}
