using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class ProductDetailData
{
    [JsonPropertyName("product")]
    public ProductFullModel? Product { get; set; }

    [JsonPropertyName("data_layer")]
    public object? DataLayer { get; set; }

    [JsonPropertyName("seo")]
    public object? Seo { get; set; }

    [JsonPropertyName("promotion_banner")]
    public object? PromotionBanner { get; set; }

    [JsonPropertyName("bigdata_tracker_data")]
    public object? BigdataTrackerData { get; set; }
}

public sealed class ProductFullModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("title_en")]
    public string? TitleEn { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }

    [JsonPropertyName("images")]
    public List<ImageModel>? Images { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rating")]
    public RatingModel? Rating { get; set; }

    [JsonPropertyName("default_variant")]
    public VariantModel? DefaultVariant { get; set; }

    [JsonPropertyName("variants")]
    public List<VariantModel>? Variants { get; set; }

    [JsonPropertyName("brand")]
    public BrandModel? Brand { get; set; }

    [JsonPropertyName("category")]
    public CategoryRef? Category { get; set; }

    [JsonPropertyName("specifications")]
    public List<SpecificationGroup>? Specifications { get; set; }

    [JsonPropertyName("expert_reviews")]
    public ExpertReviewModel? ExpertReviews { get; set; }
}

public sealed class BrandModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("title_en")]
    public string? TitleEn { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("logo")]
    public ImageModel? Logo { get; set; }
}

public sealed class SpecificationGroup
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("attributes")]
    public List<SpecificationAttribute>? Attributes { get; set; }
}

public sealed class SpecificationAttribute
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }
}

public sealed class ExpertReviewModel
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("advantages")]
    public List<string>? Advantages { get; set; }

    [JsonPropertyName("disadvantages")]
    public List<string>? Disadvantages { get; set; }
}
