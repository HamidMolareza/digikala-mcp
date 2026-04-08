using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class RankProductsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRankProducts_ByStructuredPriorities()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var searchService = CreateSearchService(apiClient, cache);
        var service = new RankProductsService(searchService);

        apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "status": 200,
                  "data": {
                    "products": [
                      {
                        "id": 10,
                        "title_fa": "گوشی موبایل A",
                        "status": "marketable",
                        "rating": { "rate": 4.6, "count": 900 },
                        "default_variant": { "price": { "selling_price": 400000000 } },
                        "url": { "uri": "/product/dkp-10" },
                        "images": []
                      },
                      {
                        "id": 20,
                        "title_fa": "گوشی موبایل B",
                        "status": "marketable",
                        "rating": { "rate": 4.8, "count": 500 },
                        "default_variant": { "price": { "selling_price": 420000000 } },
                        "url": { "uri": "/product/dkp-20" },
                        "images": []
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 },
                    "filters": [],
                    "sort_options": []
                  }
                }
                """));

        var result = await service.ExecuteAsync(
            query: "iphone",
            rankingPriority: ["buyer_popularity", "rating"],
            limit: 2,
            minRating: 4.5,
            minReviewCount: 400);

        Assert.Equal(2, result.Products.Count);
        Assert.Equal(10, result.Products[0].Id);
        Assert.Equal(1, result.Products[0].Rank);
        Assert.Contains("strong buyer popularity", string.Join(" | ", result.Products[0].WhyRankedHere), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://www.digikala.com/product/dkp-10", result.Products[0].AbsoluteUrl);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_ForUnsupportedRankingPriority()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var searchService = CreateSearchService(apiClient, cache);
        var service = new RankProductsService(searchService);

        var result = await service.ExecuteAsync(
            query: "iphone",
            rankingPriority: ["mystery_signal"]);

        Assert.NotNull(result.Error);
        Assert.Equal("InvalidInput", result.Error!.Code);
        Assert.Equal("rank_products", result.Error.Tool);
        await apiClient.DidNotReceiveWithAnyArgs().SearchCategoryAsync(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_ForInvalidFilters()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var searchService = CreateSearchService(apiClient, cache);
        var service = new RankProductsService(searchService);

        var result = await service.ExecuteAsync(
            query: "iphone",
            limit: 0,
            minPriceRials: 2_000,
            maxPriceRials: 1_000);

        Assert.NotNull(result.Error);
        Assert.Equal("InvalidInput", result.Error!.Code);
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`limit`", StringComparison.Ordinal));
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`minPriceRials` cannot be greater", StringComparison.Ordinal));
        await apiClient.DidNotReceiveWithAnyArgs().SearchCategoryAsync(default!, default, default, default, default, default);
    }

    private static SearchProductsService CreateSearchService(IDigikalaApiClient apiClient, ICacheStore cache)
    {
        var autocompleteService = new AutocompleteService(apiClient, cache, new AutocompleteMapper());
        var categoryResolutionService = new CategoryResolutionService(autocompleteService);
        return new SearchProductsService(apiClient, cache, new ProductMapper(), categoryResolutionService);
    }

    private sealed class NullCacheStore : ICacheStore
    {
        public Task CleanupExpiredAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken ct = default) where T : class => Task.CompletedTask;
    }
}
