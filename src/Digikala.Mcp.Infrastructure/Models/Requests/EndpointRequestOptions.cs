namespace Digikala.Mcp.Infrastructure.Models.Requests;

public sealed record UserInitRequestOptions(string? CamCode = null);
public sealed record FreshInitRequestOptions(int WarehouseId = 29);
public sealed record HomepageV2RequestOptions();
public sealed record DictionariesRequestOptions(string[] Types, string[]? Hashes = null);
public sealed record FreshDictionariesRequestOptions(int WarehouseId, string[] Types, string[]? Hashes = null);
public sealed record FeatureFlagsRequestOptions(string SdkKey);
public sealed record ProductDetailRequestOptions(long ProductId, long? VariantId = null);
public sealed record FreshProductDetailRequestOptions(long ProductId, int WarehouseId = 29);
public sealed record ProductCommentsRequestOptions(long ProductId, int Page = 1, string? Sort = null, int? WarehouseId = null);
public sealed record ProductPriceChartRequestOptions(long ProductId, int? WarehouseId = null);
public sealed record ProductQuestionsRequestOptions(long ProductId, int Page = 1, string? Sort = null, int? WarehouseId = null);
public sealed record ProductRecommendationRequestOptions(long ProductId, int Offset = 0);
public sealed record FreshProductRecommendationRequestOptions(long ProductId, int Offset = 0, int WarehouseId = 29);
public sealed record ProductCpcRequestOptions(long ProductId, int? WarehouseId = null, string? DeviceId = null);
public sealed record ProductPindoRequestOptions(long ProductId, int? WarehouseId = null);
public sealed record SellerVouchersRequestOptions(long ProductId);
public sealed record RateReviewRequestOptions(long ProductId, int Page = 1);
public sealed record SearchCategoryRequestOptions(
    string CategorySlug,
    int Page = 1,
    int? Sort = null,
    Dictionary<string, string[]>? Attributes = null,
    string? Query = null);
public sealed record SearchFreshCategoryRequestOptions(string CategorySlug, int Page = 1, int? Sort = null, int WarehouseId = 29);
public sealed record TagProductsRequestOptions(string TagSlug, int Page = 1);
public sealed record ProvidersProductsRequestOptions(string CategoryCode, int Page = 1, string? Query = null);
public sealed record IncredibleOffersProductsRequestOptions(int Page = 1, string? Query = null);
public sealed record CategoryLandingRequestOptions(string CategorySlug);
public sealed record IncredibleOffersRequestOptions();
public sealed record BestSellingRequestOptions(int? CategoryId = null, string? LastDays = null);
public sealed record AutocompleteRequestOptions(string Query);
public sealed record BrandPremiumRequestOptions(string BrandSlug);
public sealed record HomeRecommendationRequestOptions(string WebPage = "home");
public sealed record TrendingRecommendationRequestOptions();
public sealed record SellingAndSalesRecommendationRequestOptions();
public sealed record PersonalizedCategoryRecommendationRequestOptions(string CategorySlug, int Page = 1);
public sealed record DynamicLandingMobileRequestOptions();
public sealed record DynamicLandingWidgetRequestOptions(long WidgetId);
public sealed record MagnetHomePageRequestOptions();
public sealed record AnnouncementMessageRequestOptions(string ClientPage = "home-page");
public sealed record RelatedPostsRequestOptions(long ProductId, int? WarehouseId = null);
public sealed record CommentsOverviewUserFeedbackRequestOptions(long OverviewId);
