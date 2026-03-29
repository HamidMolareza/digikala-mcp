using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class CategoryLandingData
{
    [JsonPropertyName("category")]
    public CategoryDetailModel? Category { get; set; }

    [JsonPropertyName("sub_categories")]
    public List<SubCategoryModel>? SubCategories { get; set; }

    [JsonPropertyName("popular_brands")]
    public List<BrandModel>? PopularBrands { get; set; }

    [JsonPropertyName("best_selling_products")]
    public List<ProductListItem>? BestSellingProducts { get; set; }

    [JsonPropertyName("incredible_offers")]
    public object? IncredibleOffers { get; set; }

    [JsonPropertyName("top_repurchased_products")]
    public List<ProductListItem>? TopRepurchasedProducts { get; set; }
}

public sealed class CategoryDetailModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("title_en")]
    public string? TitleEn { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}

public sealed class SubCategoryModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}
