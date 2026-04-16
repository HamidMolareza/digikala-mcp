using System.Text.Json;

namespace Digikala.Mcp.Domain.Interfaces;

public interface IDigikalaApiClient
{
    // Session / Bootstrap
    Task<JsonDocument> GetUserInitAsync(string? camCode = null, CancellationToken ct = default);
    Task<JsonDocument> GetFreshInitAsync(int warehouseId = 29, CancellationToken ct = default);
    Task<JsonDocument> GetHomepageV2Async(CancellationToken ct = default);

    // Dictionaries
    Task<JsonDocument> GetDictionariesAsync(string[] types, string[]? hashes = null, CancellationToken ct = default);
    Task<JsonDocument> GetFreshDictionariesAsync(int warehouseId, string[] types, string[]? hashes = null, CancellationToken ct = default);

    // Feature Flags
    Task<JsonDocument> GetFeatureFlagsAsync(string sdkKey, CancellationToken ct = default);
    Task<JsonDocument> GetSuperAppPillarsAsync(CancellationToken ct = default);

    // Product Detail
    Task<JsonDocument> GetProductV2Async(long productId, long? variantId = null, CancellationToken ct = default);
    Task<JsonDocument> GetFreshProductAsync(long productId, int warehouseId = 29, CancellationToken ct = default);

    // Product Sub-Resources
    Task<JsonDocument> GetProductCommentsAsync(long productId, int page = 1, string? sort = null, int? warehouseId = null, CancellationToken ct = default);
    Task<JsonDocument> GetProductPriceChartAsync(long productId, int? warehouseId = null, CancellationToken ct = default);
    Task<JsonDocument> GetProductQuestionsAsync(long productId, int page = 1, string? sort = null, int? warehouseId = null, CancellationToken ct = default);
    Task<JsonDocument> GetProductRecommendationAsync(long productId, int offset = 0, CancellationToken ct = default);
    Task<JsonDocument> GetFreshProductRecommendationAsync(long productId, int offset = 0, int warehouseId = 29, CancellationToken ct = default);
    Task<JsonDocument> GetProductCpcAsync(long productId, int? warehouseId = null, string? deviceId = null, CancellationToken ct = default);
    Task<JsonDocument> GetProductPindoAsync(long productId, int? warehouseId = null, CancellationToken ct = default);
    Task<JsonDocument> GetProductSellerVouchersAsync(long productId, CancellationToken ct = default);

    // Rate & Review
    Task<JsonDocument> GetRateReviewAsync(long productId, int page = 1, CancellationToken ct = default);

    // Search / Browse
    Task<JsonDocument> SearchCategoryAsync(string categorySlug, int page = 1, int? sort = null, Dictionary<string, string[]>? attributes = null, string? query = null, CancellationToken ct = default);
    Task<JsonDocument> SearchFreshCategoryAsync(string categorySlug, int page = 1, int? sort = null, int warehouseId = 29, CancellationToken ct = default);
    Task<JsonDocument> GetTagProductsAsync(string tagSlug, int page = 1, CancellationToken ct = default);
    Task<JsonDocument> GetProvidersProductsAsync(string categoryCode, int page = 1, string? query = null, CancellationToken ct = default);
    Task<JsonDocument> GetIncredibleOffersProductsAsync(int page = 1, string? query = null, CancellationToken ct = default);

    // Category
    Task<JsonDocument> GetCategoryLandingAsync(string categorySlug, CancellationToken ct = default);

    // Incredible Offers
    Task<JsonDocument> GetIncredibleOffersAsync(CancellationToken ct = default);

    // Best Selling
    Task<JsonDocument> GetBestSellingAsync(int? categoryId = null, string? lastDays = null, CancellationToken ct = default);

    // Autocomplete
    Task<JsonDocument> GetAutocompleteAsync(string query, CancellationToken ct = default);

    // Brand
    Task<JsonDocument> GetBrandPremiumAsync(string brandSlug, CancellationToken ct = default);

    // Recommendations
    Task<JsonDocument> GetHomeRecommendationAsync(string webPage = "home", CancellationToken ct = default);
    Task<JsonDocument> GetTrendingRecommendationAsync(CancellationToken ct = default);
    Task<JsonDocument> GetSellingAndSalesRecommendationAsync(CancellationToken ct = default);
    Task<JsonDocument> GetPersonalizedCategoryRecommendationAsync(string categorySlug, int page = 1, CancellationToken ct = default);

    // Dynamic Landing
    Task<JsonDocument> GetDynamicLandingMobileAsync(CancellationToken ct = default);
    Task<JsonDocument> GetDynamicLandingWidgetAsync(long widgetId, CancellationToken ct = default);

    // Magnet / Content
    Task<JsonDocument> GetMagnetHomePageAsync(CancellationToken ct = default);
    Task<JsonDocument> GetAnnouncementMessageAsync(string clientPage = "home-page", CancellationToken ct = default);
    Task<JsonDocument> GetRelatedPostsAsync(long productId, int? warehouseId = null, CancellationToken ct = default);

    // Comments Overview
    Task<JsonDocument> GetCommentsOverviewUserFeedbackAsync(long overviewId, CancellationToken ct = default);
}
