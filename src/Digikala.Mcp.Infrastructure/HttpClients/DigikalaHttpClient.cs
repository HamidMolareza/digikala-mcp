using System.Text.Json;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digikala.Mcp.Infrastructure.HttpClients;

public sealed class DigikalaHttpClient(
    HttpClient httpClient,
    IOptions<DigikalaOptions> options,
    ILogger<DigikalaHttpClient> logger) : IDigikalaApiClient
{
    private readonly DigikalaOptions _options = options.Value;

    // Session / Bootstrap

    public async Task<JsonDocument> GetUserInitAsync(string? camCode = null, CancellationToken ct = default)
    {
        var url = BuildUrl("/v1/user/init/", ("camCode", camCode));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetFreshInitAsync(int warehouseId = 29, CancellationToken ct = default)
    {
        var url = BuildUrl("/fresh/v1/init/", ("_whid", warehouseId.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetHomepageV2Async(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v2/", ct);
    }

    // Dictionaries

    public async Task<JsonDocument> GetDictionariesAsync(string[] types, string[]? hashes = null, CancellationToken ct = default)
    {
        var query = BuildArrayQuery("types", types, "hashes", hashes);
        return await GetJsonAsync($"/v1/dictionaries/?{query}", ct);
    }

    public async Task<JsonDocument> GetFreshDictionariesAsync(int warehouseId, string[] types, string[]? hashes = null, CancellationToken ct = default)
    {
        var query = BuildArrayQuery("types", types, "hashes", hashes);
        return await GetJsonAsync($"/fresh/v1/dictionaries/?_whid={warehouseId}&{query}", ct);
    }

    // Feature Flags

    public async Task<JsonDocument> GetFeatureFlagsAsync(string sdkKey, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/api/features/{Uri.EscapeDataString(sdkKey)}", ct, useFeatureHost: true);
    }

    // Product Detail

    public async Task<JsonDocument> GetProductV2Async(long productId, long? variantId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v2/product/{productId}/", ("variant_id", variantId?.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetFreshProductAsync(long productId, int warehouseId = 29, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/fresh/v1/product/{productId}/?_whid={warehouseId}", ct);
    }

    // Product Sub-Resources

    public async Task<JsonDocument> GetProductCommentsAsync(long productId, int page = 1, string? sort = null, int? warehouseId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/product/{productId}/comments/",
            ("page", page.ToString()), ("sort", sort), ("_whid", warehouseId?.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetProductPriceChartAsync(long productId, int? warehouseId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/product/{productId}/price-chart/", ("_whid", warehouseId?.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetProductQuestionsAsync(long productId, int page = 1, string? sort = null, int? warehouseId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/product/{productId}/questions/",
            ("page", page.ToString()), ("sort", sort), ("_whid", warehouseId?.ToString()),
            ("productId", productId.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetProductRecommendationAsync(long productId, int offset = 0, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/product/{productId}/recommendation/?offset={offset}", ct);
    }

    public async Task<JsonDocument> GetFreshProductRecommendationAsync(long productId, int offset = 0, int warehouseId = 29, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/fresh/v1/product/{productId}/recommendation/?_whid={warehouseId}&offset={offset}", ct);
    }

    public async Task<JsonDocument> GetProductCpcAsync(long productId, int? warehouseId = null, string? deviceId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/product/{productId}/cpc/", ("_whid", warehouseId?.ToString()), ("deviceId", deviceId));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetProductPindoAsync(long productId, int? warehouseId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/product/{productId}/pindo/", ("_whid", warehouseId?.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetProductSellerVouchersAsync(long productId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/product/{productId}/seller-vouchers/?productId={productId}", ct);
    }

    public async Task<JsonDocument> GetProductDigipayCreditAsync(long productId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/product/{productId}/digipay-credit/?productId={productId}", ct);
    }

    // Rate & Review

    public async Task<JsonDocument> GetRateReviewAsync(long productId, int page = 1, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/rate-review/products/{productId}/?page={page}", ct);
    }

    // Search / Browse

    public async Task<JsonDocument> SearchCategoryAsync(string categorySlug, int page = 1, int? sort = null,
        Dictionary<string, string[]>? attributes = null, string? query = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/categories/{Uri.EscapeDataString(categorySlug)}/search/",
            ("page", page.ToString()), ("sort", sort?.ToString()), ("q", query));
        if (attributes is not null)
        {
            foreach (var (key, values) in attributes)
            {
                for (int i = 0; i < values.Length; i++)
                    url += $"&attributes%5B{key}%5D%5B{i}%5D={Uri.EscapeDataString(values[i])}";
            }
        }
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> SearchFreshCategoryAsync(string categorySlug, int page = 1, int? sort = null,
        int warehouseId = 29, CancellationToken ct = default)
    {
        var url = BuildUrl($"/fresh/v1/categories/{Uri.EscapeDataString(categorySlug)}/search/",
            ("_whid", warehouseId.ToString()), ("page", page.ToString()), ("sort", sort?.ToString()));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetTagProductsAsync(string tagSlug, int page = 1, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/tags/{Uri.EscapeDataString(tagSlug)}/?page={page}", ct);
    }

    public async Task<JsonDocument> GetProvidersProductsAsync(string categoryCode, int page = 1, string? query = null, CancellationToken ct = default)
    {
        var url = BuildUrl("/v1/providers-products/",
            ("category_code", categoryCode), ("page", page.ToString()), ("q", query));
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonDocument> GetIncredibleOffersProductsAsync(int page = 1, string? query = null, CancellationToken ct = default)
    {
        var url = BuildUrl("/v1/incredible-offers/products/", ("page", page.ToString()), ("q", query));
        return await GetJsonAsync(url, ct);
    }

    // Category

    public async Task<JsonDocument> GetCategoryLandingAsync(string categorySlug, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/categories/{Uri.EscapeDataString(categorySlug)}/", ct);
    }

    // Incredible Offers

    public async Task<JsonDocument> GetIncredibleOffersAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v1/incredible-offers/", ct);
    }

    // Best Selling

    public async Task<JsonDocument> GetBestSellingAsync(int? categoryId = null, string? lastDays = null, CancellationToken ct = default)
    {
        var url = BuildUrl("/v1/best-selling/", ("category_id", categoryId?.ToString()), ("last_days", lastDays));
        return await GetJsonAsync(url, ct);
    }

    // Autocomplete

    public async Task<JsonDocument> GetAutocompleteAsync(string query, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/autocomplete/?q={Uri.EscapeDataString(query)}", ct);
    }

    // Brand

    public async Task<JsonDocument> GetBrandPremiumAsync(string brandSlug, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/brands/{Uri.EscapeDataString(brandSlug)}/premium/", ct);
    }

    // Recommendations

    public async Task<JsonDocument> GetHomeRecommendationAsync(string webPage = "home", CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/recommendation/?web_page={Uri.EscapeDataString(webPage)}", ct);
    }

    public async Task<JsonDocument> GetTrendingRecommendationAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v1/recommendation/trending/?endpoint=%2Frecommendation%2Ftrending%2F", ct);
    }

    public async Task<JsonDocument> GetSellingAndSalesRecommendationAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v1/recommendation/selling-and-sales/?endpoint=%2Frecommendation%2Fselling-and-sales%2F", ct);
    }

    public async Task<JsonDocument> GetPersonalizedCategoryRecommendationAsync(string categorySlug, int page = 1, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/recommendation/personalized-category/{Uri.EscapeDataString(categorySlug)}/?page={page}", ct);
    }

    // Dynamic Landing

    public async Task<JsonDocument> GetDynamicLandingMobileAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v1/dynamic-landing/mobile/", ct);
    }

    public async Task<JsonDocument> GetDynamicLandingWidgetAsync(long widgetId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/dynamic-landing/widget/{widgetId}/", ct);
    }

    // Magnet / Content

    public async Task<JsonDocument> GetMagnetHomePageAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/v1/magnet/home-page/", ct);
    }

    public async Task<JsonDocument> GetAnnouncementMessageAsync(string clientPage = "home-page", CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/magnet/notification-center/announcement/message/?clientPage={Uri.EscapeDataString(clientPage)}", ct);
    }

    public async Task<JsonDocument> GetRelatedPostsAsync(long productId, int? warehouseId = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"/v1/magnet/related-posts/{productId}/",
            ("_whid", warehouseId?.ToString()), ("productId", productId.ToString()));
        return await GetJsonAsync(url, ct);
    }

    // Comments Overview

    public async Task<JsonDocument> GetCommentsOverviewUserFeedbackAsync(long overviewId, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/v1/comments-overview/{overviewId}/user-feedback/?overviewId={overviewId}", ct);
    }

    // ── Private helpers ──

    private async Task<JsonDocument> GetJsonAsync(string relativeUrl, CancellationToken ct, bool useFeatureHost = false)
    {
        var baseUrl = useFeatureHost ? _options.FeaturesBaseUrl : _options.BaseUrl;
        var requestUri = $"{baseUrl.TrimEnd('/')}{relativeUrl}";

        logger.LogDebug("GET {Url}", requestUri);

        var response = await httpClient.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private static string BuildUrl(string path, params (string key, string? value)[] queryParams)
    {
        var filtered = queryParams.Where(p => p.value is not null).ToArray();
        if (filtered.Length == 0) return path;

        var separator = path.Contains('?') ? '&' : '?';
        var query = string.Join('&', filtered.Select(p => $"{p.key}={Uri.EscapeDataString(p.value!)}"));
        return $"{path}{separator}{query}";
    }

    private static string BuildArrayQuery(string typesKey, string[] types, string hashesKey, string[]? hashes)
    {
        var parts = new List<string>();
        for (int i = 0; i < types.Length; i++)
            parts.Add($"{typesKey}%5B{i}%5D={Uri.EscapeDataString(types[i])}");
        if (hashes is not null)
        {
            for (int i = 0; i < hashes.Length; i++)
                parts.Add($"{hashesKey}%5B{i}%5D={Uri.EscapeDataString(hashes[i])}");
        }
        return string.Join('&', parts);
    }
}
