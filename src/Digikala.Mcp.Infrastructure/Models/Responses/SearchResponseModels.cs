using System.Text.Json.Serialization;
using System.Text.Json;
using Digikala.Mcp.Infrastructure.Models.Common;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class SearchResponseData
{
    [JsonPropertyName("products")]
    public List<ProductListItem>? Products { get; set; }

    [JsonPropertyName("filters")]
    public Dictionary<string, JsonElement>? Filters { get; set; }

    [JsonPropertyName("sort")]
    public Dictionary<string, JsonElement>? Sort { get; set; }

    [JsonPropertyName("sort_options")]
    public List<SortOptionItem>? SortOptions { get; set; }

    [JsonPropertyName("pager")]
    public PagerModel? Pager { get; set; }

    [JsonPropertyName("result_type")]
    public string? ResultType { get; set; }
}

public sealed class ProductListItem
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
    public ImagesModel? Images { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rating")]
    public RatingModel? Rating { get; set; }

    [JsonPropertyName("default_variant")]
    public VariantModel? DefaultVariant { get; set; }

    [JsonPropertyName("category")]
    public CategoryRef? Category { get; set; }
}

public sealed class UrlModel
{
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}

public sealed class ImageModel
{
    [JsonPropertyName("url")]
    public List<string>? Url { get; set; }
}

public sealed class ImagesModel
{
    [JsonPropertyName("main")]
    public ImageModel? Main { get; set; }

    [JsonPropertyName("list")]
    public List<ImageModel>? List { get; set; }
}

public sealed class RatingModel
{
    [JsonPropertyName("rate")]
    public double Rate { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class VariantModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("price")]
    public PriceModel? Price { get; set; }

    [JsonPropertyName("seller")]
    public SellerModel? Seller { get; set; }

    [JsonPropertyName("warranty")]
    public WarrantyModel? Warranty { get; set; }

    [JsonPropertyName("color")]
    public ColorModel? Color { get; set; }
}

public sealed class PriceModel
{
    [JsonPropertyName("selling_price")]
    public long SellingPrice { get; set; }

    [JsonPropertyName("rrp_price")]
    public long? RrpPrice { get; set; }

    [JsonPropertyName("discount_percent")]
    public int? DiscountPercent { get; set; }
}

public sealed class SellerModel
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("rating")]
    public SellerRatingModel? Rating { get; set; }
}

public sealed class SellerRatingModel
{
    [JsonPropertyName("total_rate")]
    public double? TotalRate { get; set; }

    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("commitment")]
    public double? Commitment { get; set; }

    [JsonPropertyName("no_return")]
    public double? NoReturn { get; set; }

    [JsonPropertyName("on_time_shipping")]
    public double? OnTimeShipping { get; set; }
}

public sealed class WarrantyModel
{
    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }
}

public sealed class ColorModel
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("hex_code")]
    public string? HexCode { get; set; }
}

public sealed class CategoryRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }
}

public sealed class FilterGroup
{
    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("options")]
    public List<FilterOptionItem>? Options { get; set; }
}

public sealed class FilterOptionItem
{
    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class SortOptionItem
{
    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}
