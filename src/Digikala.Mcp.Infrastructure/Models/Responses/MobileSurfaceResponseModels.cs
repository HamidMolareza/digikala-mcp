using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class DynamicLandingMobileData
{
    [JsonPropertyName("landing_id")]
    public int LandingId { get; set; }

    [JsonPropertyName("logged_in")]
    public bool LoggedIn { get; set; }

    [JsonPropertyName("seo")]
    public LandingSeoModel? Seo { get; set; }

    [JsonPropertyName("studio_signed")]
    public string? StudioSigned { get; set; }

    [JsonPropertyName("widgets")]
    public List<DynamicLandingWidgetModel>? Widgets { get; set; }
}

public sealed class LandingSeoModel
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("header")]
    public LandingHeaderModel? Header { get; set; }
}

public sealed class LandingHeaderModel
{
    [JsonPropertyName("robots_meta")]
    public string? RobotsMeta { get; set; }
}

public sealed class DynamicLandingWidgetModel
{
    [JsonPropertyName("widget_type")]
    public string? WidgetType { get; set; }

    [JsonPropertyName("widget_id")]
    public long WidgetId { get; set; }

    [JsonPropertyName("data")]
    public DynamicLandingWidgetDataModel? Data { get; set; }

    [JsonPropertyName("sort")]
    public int? Sort { get; set; }
}

public sealed class DynamicLandingWidgetDataModel
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("home_page_slider")]
    public bool? HomePageSlider { get; set; }

    [JsonPropertyName("is_loop")]
    public bool? IsLoop { get; set; }

    [JsonPropertyName("is_auto_play")]
    public bool? IsAutoPlay { get; set; }

    [JsonPropertyName("auto_play_delay")]
    public int? AutoPlayDelay { get; set; }

    [JsonPropertyName("is_full")]
    public bool? IsFull { get; set; }

    [JsonPropertyName("mobile_slides")]
    public List<DynamicLandingSlideModel>? MobileSlides { get; set; }

    [JsonPropertyName("desktop_slides")]
    public List<DynamicLandingSlideModel>? DesktopSlides { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("image_per_row")]
    public int? ImagePerRow { get; set; }

    [JsonPropertyName("images")]
    public List<DynamicLandingImageRowItemModel>? Images { get; set; }

    [JsonPropertyName("widget_url")]
    public string? WidgetUrl { get; set; }

    [JsonPropertyName("discount_percent")]
    public int? DiscountPercent { get; set; }

    [JsonPropertyName("see_more_url")]
    public UrlModel? SeeMoreUrl { get; set; }

    [JsonPropertyName("products")]
    public List<ProductListItem>? Products { get; set; }

    [JsonPropertyName("product_data")]
    public ProductListItem? ProductData { get; set; }
}

public sealed class DynamicLandingSlideModel
{
    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }

    [JsonPropertyName("image")]
    public DynamicLandingImageAssetModel? Image { get; set; }
}

public sealed class DynamicLandingImageRowItemModel
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("image")]
    public DynamicLandingImageAssetModel? Image { get; set; }

    [JsonPropertyName("url")]
    public UrlModel? Url { get; set; }
}

public sealed class DynamicLandingImageAssetModel
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("webp_url")]
    public string? WebpUrl { get; set; }
}

public sealed class MagnetHomePageData
{
    [JsonPropertyName("posts")]
    public List<MagnetPostModel>? Posts { get; set; }
}

public sealed class MagnetPostModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("category_ids")]
    public List<int>? CategoryIds { get; set; }

    [JsonPropertyName("tag_ids")]
    public List<int>? TagIds { get; set; }

    [JsonPropertyName("user_setting_id")]
    public string? UserSettingId { get; set; }

    [JsonPropertyName("related_product_ids")]
    public List<long>? RelatedProductIds { get; set; }

    [JsonPropertyName("views_count")]
    public long? ViewsCount { get; set; }

    [JsonPropertyName("likes_count")]
    public long? LikesCount { get; set; }

    [JsonPropertyName("comments_count")]
    public long? CommentsCount { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("author")]
    public MagnetAuthorModel? Author { get; set; }

    [JsonPropertyName("media")]
    public List<MagnetMediaModel>? Media { get; set; }

    [JsonPropertyName("tags")]
    public List<MagnetTagModel>? Tags { get; set; }

    [JsonPropertyName("products")]
    public List<MagnetProductModel>? Products { get; set; }

    [JsonPropertyName("share_url")]
    public string? ShareUrl { get; set; }

    [JsonPropertyName("circle_cover")]
    public string? CircleCover { get; set; }
}

public sealed class MagnetAuthorModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("photo")]
    public string? Photo { get; set; }

    [JsonPropertyName("badge")]
    public MagnetBadgeModel? Badge { get; set; }

    [JsonPropertyName("marketplace_seller_id")]
    public long? MarketplaceSellerId { get; set; }

    [JsonPropertyName("seller_info")]
    public MagnetSellerInfoModel? SellerInfo { get; set; }
}

public sealed class MagnetBadgeModel
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

public sealed class MagnetSellerInfoModel
{
    [JsonPropertyName("seller_id")]
    public long? SellerId { get; set; }

    [JsonPropertyName("seller_name")]
    public string? SellerName { get; set; }
}

public sealed class MagnetMediaModel
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("ratio")]
    public double? Ratio { get; set; }

    [JsonPropertyName("cover_small")]
    public string? CoverSmall { get; set; }

    [JsonPropertyName("cover_medium")]
    public string? CoverMedium { get; set; }

    [JsonPropertyName("cover_big")]
    public string? CoverBig { get; set; }
}

public sealed class MagnetTagModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public sealed class MagnetProductModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title_fa")]
    public string? TitleFa { get; set; }

    [JsonPropertyName("images")]
    public MagnetProductImagesModel? Images { get; set; }

    [JsonPropertyName("price")]
    public PriceModel? Price { get; set; }
}

public sealed class MagnetProductImagesModel
{
    [JsonPropertyName("main")]
    public string? Main { get; set; }
}
