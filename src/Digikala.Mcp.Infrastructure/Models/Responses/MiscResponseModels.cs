using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class BrandPremiumData
{
    [JsonPropertyName("brand")]
    public BrandModel? Brand { get; set; }

    [JsonPropertyName("newest_products")]
    public List<ProductListItem>? NewestProducts { get; set; }

    [JsonPropertyName("best_selling_products")]
    public List<ProductListItem>? BestSellingProducts { get; set; }

    [JsonPropertyName("header_banners")]
    public List<object>? HeaderBanners { get; set; }

    [JsonPropertyName("top_banners")]
    public List<object>? TopBanners { get; set; }
}

public sealed class UserInitData
{
    [JsonPropertyName("is_logged_in")]
    public bool IsLoggedIn { get; set; }

    [JsonPropertyName("show_location_modal")]
    public bool? ShowLocationModal { get; set; }

    [JsonPropertyName("modules")]
    public object? Modules { get; set; }

    [JsonPropertyName("date_time")]
    public object? DateTime { get; set; }

    [JsonPropertyName("social_profile")]
    public object? SocialProfile { get; set; }
}

public sealed class FreshInitData
{
    [JsonPropertyName("is_logged_in")]
    public bool IsLoggedIn { get; set; }

    [JsonPropertyName("default_warehouse_id")]
    public int DefaultWarehouseId { get; set; }

    [JsonPropertyName("modules")]
    public object? Modules { get; set; }

    [JsonPropertyName("date_time")]
    public object? DateTime { get; set; }

    [JsonPropertyName("merge_version")]
    public string? MergeVersion { get; set; }
}

public sealed class SellerVouchersData
{
    [JsonPropertyName("vouchers")]
    public List<SellerVoucherModel>? Vouchers { get; set; }
}

public sealed class SellerVoucherModel
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("min_purchase")]
    public long? MinPurchase { get; set; }

    [JsonPropertyName("discount_percent")]
    public int? DiscountPercent { get; set; }

    [JsonPropertyName("max_discount")]
    public long? MaxDiscount { get; set; }
}

public sealed class HomepageV2Data
{
    [JsonPropertyName("widgets")]
    public List<object>? Widgets { get; set; }

    [JsonPropertyName("seo")]
    public object? Seo { get; set; }

    [JsonPropertyName("recommendation_version")]
    public string? RecommendationVersion { get; set; }
}
