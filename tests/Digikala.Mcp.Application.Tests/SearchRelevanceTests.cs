using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class SearchRelevanceTests
{
    private readonly IDigikalaApiClient _apiClient = Substitute.For<IDigikalaApiClient>();
    private readonly ICacheStore _cache = new NullCacheStore();

    [Fact]
    public async Task ExecuteAsync_ShouldRankKeywordMatchesAboveHigherRatedUnrelatedProducts()
    {
        SetupPages(CategoryPage(
            totalPages: 1,
            Product(1, "ماگ حرارتی طرح گربه", rate: 95, count: 5000),
            Product(2, "ماگ استیل دو‌جداره دسته دار", rate: 78, count: 40)));

        var result = await CreateService().ExecuteAsync("ماگ دوجداره استیل", "mug");

        Assert.Equal(2, result.BestMatch!.Id);
        Assert.True(result.Products[0].SearchScore > result.Products[1].SearchScore);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTreatPercentRatingsOnFivePointScale()
    {
        SetupPages(CategoryPage(
            totalPages: 1,
            Product(1, "ماگ", rate: 92, count: 100),
            Product(2, "ماگ", rate: 80, count: 100)));

        var result = await CreateService().ExecuteAsync("ماگ", "mug", minRating: 4.5);

        Assert.Equal(1, Assert.Single(result.Products).Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReadMorePages_WhenLocalFiltersLeaveTooFewProducts()
    {
        SetupPages(
            CategoryPage(totalPages: 5, Product(1, "ماگ", rate: 90, count: 2), Product(2, "ماگ", rate: 90, count: 3)),
            CategoryPage(totalPages: 5, Product(3, "ماگ", rate: 90, count: 500)),
            CategoryPage(totalPages: 5, Product(4, "ماگ", rate: 90, count: 600)),
            CategoryPage(totalPages: 5, Product(5, "ماگ", rate: 90, count: 700)));

        var result = await CreateService().ExecuteAsync("ماگ", "mug", minReviewCount: 100, limit: 5);

        Assert.Equal(3, result.PagesFetched);
        Assert.Equal([3L, 4L], result.Products.Select(item => item.Id).Order().ToArray());
        await _apiClient.Received(3).SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepDigikalaOrder_WhenCallerAsksForNonRelevanceSort()
    {
        SetupPages(CategoryPage(
            totalPages: 1,
            Product(1, "کابل", rate: 60, count: 1, price: 1_000),
            Product(2, "ماگ دوجداره", rate: 99, count: 9000, price: 5_000)));

        var result = await CreateService().ExecuteAsync("ماگ دوجداره", "mug", sort: 20);

        Assert.Equal([1L, 2L], result.Products.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendPriceAndStockFiltersUpstream()
    {
        SetupPages(CategoryPage(totalPages: 1, Product(1, "ماگ", rate: 90, count: 10)));

        await CreateService().ExecuteAsync("ماگ", "mug", onlyAvailable: true, maxPriceRials: 10_000_000);

        await _apiClient.Received(1).SearchProductsAsync(
            Arg.Is<ProductSearchRequest>(r =>
                r.CategorySlug == "mug" && r.OnlyAvailable && r.MaxPriceRials == 10_000_000 && r.MinPriceRials == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AutocompleteMapper_ShouldReadDiscoveryShape_AndExtractCategorySlugFromUrl()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "auto_complete": [
                  { "type": "category", "id": 10707, "title": "ماگ فانتزی", "category_title": "ماگ", "url": { "url": "/search/category-mug/?q=x" } },
                  { "type": "query", "title": "ماگ دوجداره", "url": { "url": "/search/?q=y" } }
                ],
                "trends": [{ "keyword": "عروسک " }]
              }
            }
            """);

        var result = new AutocompleteMapper().MapToAutocomplete(document);

        Assert.Equal(2, result.Suggestions.Count);
        Assert.Equal("mug", result.Suggestions[0].CategorySlug);
        Assert.Equal("category", result.Suggestions[0].Type);
        Assert.Equal("ماگ", result.Suggestions[0].CategoryTitle);
        Assert.Null(result.Suggestions[1].CategorySlug);
        Assert.Equal(["عروسک"], result.TrendingSearches);
    }

    [Fact]
    public void CategoryMapper_ShouldBuildLandingFromBestSellingSearch()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "category": { "id": 11, "title_fa": "گوشی موبایل", "code": "mobile-phone" },
                "products": [
                  { "id": 1, "title_fa": "A", "status": "marketable", "data_layer": { "brand": "سامسونگ" }, "url": { "uri": "/a" } },
                  { "id": 2, "title_fa": "B", "status": "marketable", "data_layer": { "brand": "اپل" }, "url": { "uri": "/b" } },
                  { "id": 3, "title_fa": "C", "status": "marketable", "data_layer": { "brand": "سامسونگ" }, "url": { "uri": "/c" } },
                  { "id": 4, "title_fa": "D", "status": "marketable", "data_layer": { "brand": "متفرقه" }, "url": { "uri": "/d" } }
                ]
              }
            }
            """);

        var result = new CategoryMapper(new ProductMapper()).MapToCategoryFromSearch(document);

        Assert.Equal("mobile-phone", result.Slug);
        Assert.Equal("گوشی موبایل", result.Title);
        Assert.Equal("/search/category-mobile-phone/", result.Url);
        Assert.Equal(4, result.BestSellingProducts.Count);
        Assert.Equal(["سامسونگ", "اپل"], result.PopularBrands.Select(item => item.Title).ToArray());
    }

    private SearchProductsService CreateService()
    {
        var autocompleteService = new AutocompleteService(_apiClient, _cache, new AutocompleteMapper());
        return new SearchProductsService(_apiClient, _cache, new ProductMapper(), new CategoryResolutionService(autocompleteService));
    }

    private void SetupPages(params string[] pages)
    {
        _apiClient.SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ProductSearchRequest>();
                return Task.FromResult(JsonDocument.Parse(pages[Math.Min(request.Page, pages.Length) - 1]));
            });
    }

    private static string CategoryPage(int totalPages, params string[] products) => $$"""
        {
          "status": 200,
          "data": {
            "products": [{{string.Join(',', products)}}],
            "pager": { "current_page": 1, "total_pages": {{totalPages}}, "total_items": {{totalPages * 20}} },
            "sort_options": []
          }
        }
        """;

    private static string Product(long id, string title, double rate, int count, long price = 1_000_000) => $$"""
        {
          "id": {{id}},
          "title_fa": "{{title}}",
          "status": "marketable",
          "rating": { "rate": {{rate}}, "count": {{count}} },
          "default_variant": { "price": { "selling_price": {{price}} } },
          "url": { "uri": "/product/dkp-{{id}}/" },
          "images": []
        }
        """;

    private sealed class NullCacheStore : ICacheStore
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);

        public Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken ct = default) where T : class => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;

        public Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default) => Task.CompletedTask;

        public Task CleanupExpiredAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
