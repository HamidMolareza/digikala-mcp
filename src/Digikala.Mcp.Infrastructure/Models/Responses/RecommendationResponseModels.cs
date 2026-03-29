using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class RecommendationResponseData
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public RecommendationInnerData? Data { get; set; }

    [JsonPropertyName("recommendation_version")]
    public string? RecommendationVersion { get; set; }
}

public sealed class RecommendationInnerData
{
    [JsonPropertyName("products")]
    public List<ProductListItem>? Products { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public sealed class HomeRecommendationData
{
    [JsonPropertyName("categories")]
    public List<RecommendationCategoryItem>? Categories { get; set; }

    [JsonPropertyName("recommendation_version")]
    public string? RecommendationVersion { get; set; }
}

public sealed class RecommendationCategoryItem
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}

public sealed class TrendingResponseData
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("products")]
    public List<ProductListItem>? Products { get; set; }
}
