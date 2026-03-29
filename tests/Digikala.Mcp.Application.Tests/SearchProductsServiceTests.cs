using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class SearchProductsServiceTests
{
    private readonly IDigikalaApiClient _apiClient = Substitute.For<IDigikalaApiClient>();
    private readonly ICacheStore _cacheStore = Substitute.For<ICacheStore>();
    private readonly SearchProductsService _service;

    public SearchProductsServiceTests()
    {
        _service = new SearchProductsService(_apiClient, _cacheStore, new ProductMapper());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCachedValue_WhenPresent()
    {
        var cached = new SearchResultDto { TotalCount = 7 };
        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _service.ExecuteAsync("iphone", "mobile-phone");

        Assert.Same(cached, result);
        Assert.True(result.IsFromCache);
        Assert.Equal("cache", result.CacheStatus);
        await _apiClient.DidNotReceiveWithAnyArgs().SearchCategoryAsync(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInferCategorySlug_ForRecognizedQueries()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "گوشی موبایل",
                    "status": "marketable",
                    "rating": { "rate": 4.7, "count": 650 },
                    "default_variant": { "price": { "selling_price": 400000000 } },
                    "url": { "uri": "/a" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync("iphone.");

        Assert.Single(result.Products);
        Assert.Contains("Category inferred as `mobile-phone`", result.Warning);
        await _apiClient.Received(1).SearchCategoryAsync(
            "mobile-phone",
            Arg.Any<int>(),
            Arg.Any<int?>(),
            Arg.Any<Dictionary<string, string[]>?>(),
            "iphone",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStructuredValidationError_WhenCategoryCannotBeResolved()
    {
        var result = await _service.ExecuteAsync("random obscure query");

        Assert.NotNull(result.Error);
        Assert.Equal("InvalidInput", result.Error!.Code);
        Assert.Contains("requires categorySlug", result.Error.Message, StringComparison.Ordinal);
        Assert.False(result.Error.Retryable);
        Assert.Equal(0, result.FilteredCount);
        await _apiClient.DidNotReceiveWithAnyArgs().SearchCategoryAsync(default!, default, default, default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldServeStaleValue_WhenUpstreamFails()
    {
        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _cacheStore.GetStaleAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultDto { TotalCount = 3 });
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("boom"));

        var result = await _service.ExecuteAsync("iphone", "mobile-phone");

        Assert.True(result.IsStale);
        Assert.True(result.IsFromCache);
        Assert.Equal("stale-cache", result.CacheStatus);
        Assert.NotNull(result.Warning);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBypassCache_WhenSkipCacheRequested()
    {
        var cached = new SearchResultDto
        {
            TotalCount = 1,
            Products = [new ProductSummaryDto { Id = 99, Title = "cached" }],
        };
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "live",
                    "status": "marketable",
                    "rating": { "rate": 4.7, "count": 650 },
                    "default_variant": { "price": { "selling_price": 400000000 } },
                    "url": { "uri": "/a" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync("iphone.", "mobile-phone", skipCache: true);

        Assert.Equal(1, result.Products[0].Id);
        Assert.False(result.IsFromCache);
        Assert.Equal("bypassed", result.CacheStatus);
        await _apiClient.Received(1).SearchCategoryAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int?>(),
            Arg.Any<Dictionary<string, string[]>?>(),
            Arg.Is<string?>(value => value == "iphone"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterAndRankProducts()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "A",
                    "status": "marketable",
                    "rating": { "rate": 4.7, "count": 650 },
                    "default_variant": { "price": { "selling_price": 400000000 } },
                    "url": { "uri": "/a" },
                    "images": []
                  },
                  {
                    "id": 2,
                    "title_fa": "B",
                    "status": "marketable",
                    "rating": { "rate": 4.6, "count": 1200 },
                    "default_variant": { "price": { "selling_price": 350000000 } },
                    "url": { "uri": "/b" },
                    "images": []
                  },
                  {
                    "id": 3,
                    "title_fa": "C",
                    "status": "unavailable",
                    "rating": { "rate": 4.9, "count": 50 },
                    "default_variant": { "price": { "selling_price": 250000000 } },
                    "url": { "uri": "/c" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 3 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync(
            "iphone",
            "mobile-phone",
            minRating: 4.5,
            minReviewCount: 500,
            onlyAvailable: true,
            limit: 2);

        Assert.Equal(2, result.Products.Count);
        Assert.Equal(2, result.FilteredCount);
        Assert.NotNull(result.BestMatch);
        Assert.Equal(1, result.BestMatch!.Id);
        Assert.Contains("min_rating>=4.5", result.AppliedFilters);
        Assert.Contains("min_review_count>=500", result.AppliedFilters);
        Assert.All(result.Products, item => Assert.True(item.SearchScore > 0));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBoostPreferredTerms_AndHideFilterMetadata_WhenRequested()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "شامپو کنترل کننده چربی قوی",
                    "status": "marketable",
                    "rating": { "rate": 4.8, "count": 900 },
                    "default_variant": { "price": { "selling_price": 2500000 } },
                    "url": { "uri": "/a" },
                    "images": []
                  },
                  {
                    "id": 2,
                    "title_fa": "شامپو روزانه ملایم بدون سولفات برای موهای چرب",
                    "status": "marketable",
                    "rating": { "rate": 4.6, "count": 800 },
                    "default_variant": { "price": { "selling_price": 2600000 } },
                    "url": { "uri": "/b" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 },
                "filters": [{ "title_fa": "برند", "code": "brand", "options": [] }],
                "sort_options": [{ "title_fa": "مرتبط ترین" }]
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync(
            "شامپو",
            "hair-shampoo",
            preferTerms: ["ملایم", "روزانه", "بدون سولفات"],
            avoidTerms: ["کنترل کننده چربی قوی"],
            includeAvailableFilters: false,
            preferBalancedMatches: true);

        Assert.Equal(2, result.BestMatch!.Id);
        Assert.Contains("ملایم", result.BestMatch.MatchedSignals);
        Assert.Contains("روزانه", result.BestMatch.MatchedSignals);
        Assert.Contains("بدون سولفات", result.BestMatch.MatchedSignals);
        Assert.Contains("daily use", result.BestMatch.UseCaseHints);
        Assert.True(result.BestMatch.IsSulfateFree);
        Assert.True(result.BestMatch.IsDailyUse);
        Assert.Empty(result.AvailableFilters);
        Assert.Empty(result.AvailableSortOptions);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStructuredError_WhenUpstreamAndCacheFail()
    {
        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _cacheStore.GetStaleAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 500 for https://api.digikala.com/v1/categories/mobile-phone/search/?page=1&q=iphone"));

        var result = await _service.ExecuteAsync("iphone", "mobile-phone");

        Assert.NotNull(result.Error);
        Assert.Equal("search_products", result.Error!.Tool);
        Assert.Equal(500, result.Error.StatusCode);
        Assert.True(result.Error.Retryable);
        Assert.Equal(0, result.FilteredCount);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFreshResult_WhenCacheWriteFails()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "گوشی موبایل",
                    "status": "marketable",
                    "rating": { "rate": 4.7, "count": 650 },
                    "default_variant": { "price": { "selling_price": 400000000 } },
                    "url": { "uri": "/a" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _cacheStore.SetAsync(Arg.Any<string>(), Arg.Any<SearchResultDto>(), Arg.Any<Digikala.Mcp.Domain.Enums.CacheTier>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("cache write failed"));
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync("iphone", "mobile-phone");

        Assert.Single(result.Products);
        Assert.Equal(1, result.TotalCount);
        await _apiClient.Received(1).SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReuseCacheKey_WhenPreferredTermsOrderDiffers()
    {
        var cache = new MemoryCacheStore();
        var service = new SearchProductsService(_apiClient, cache, new ProductMapper());
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "شامپو روزانه ملایم بدون سولفات",
                    "status": "marketable",
                    "rating": { "rate": 4.6, "count": 800 },
                    "default_variant": { "price": { "selling_price": 2600000 } },
                    "url": { "uri": "/b" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var first = await service.ExecuteAsync(
            "شامپو",
            "hair-shampoo",
            preferTerms: ["ملایم", "روزانه", "بدون سولفات"],
            avoidTerms: ["کنترل کننده چربی قوی", "ضد شوره"]);
        var second = await service.ExecuteAsync(
            "شامپو",
            "hair-shampoo",
            preferTerms: ["بدون سولفات", "روزانه", "ملایم"],
            avoidTerms: ["ضد شوره", "کنترل کننده چربی قوی"]);

        Assert.Single(first.Products);
        Assert.Single(second.Products);
        await _apiClient.Received(1).SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInferCategorySlug_ForCategoryLessShampooQueries()
    {
        using var document = JsonDocument.Parse("""
            {
              "status": 200,
              "data": {
                "products": [
                  {
                    "id": 1,
                    "title_fa": "شامپو",
                    "status": "marketable",
                    "rating": { "rate": 4.6, "count": 80 },
                    "default_variant": { "price": { "selling_price": 2500000 } },
                    "url": { "uri": "/b" },
                    "images": []
                  }
                ],
                "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                "filters": [],
                "sort_options": []
              }
            }
            """);

        _cacheStore.GetAsync<SearchResultDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SearchResultDto?)null);
        _apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _service.ExecuteAsync("شامپو");

        Assert.Single(result.Products);
        Assert.Contains("Category inferred as `hair-shampoo`", result.Warning);
        await _apiClient.Received(1).SearchCategoryAsync(
            "hair-shampoo",
            Arg.Any<int>(),
            Arg.Any<int?>(),
            Arg.Any<Dictionary<string, string[]>?>(),
            "شامپو",
            Arg.Any<CancellationToken>());
    }

    private sealed class MemoryCacheStore : ICacheStore
    {
        private readonly Dictionary<string, object> _entries = new(StringComparer.Ordinal);

        public Task CleanupExpiredAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class =>
            Task.FromResult(_entries.TryGetValue(key, out var value) ? value as T : null);

        public Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class =>
            Task.FromResult(_entries.TryGetValue(key, out var value) ? value as T : null);

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _entries.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByTierAsync(Digikala.Mcp.Domain.Enums.CacheTier tier, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SetAsync<T>(string key, T value, Digikala.Mcp.Domain.Enums.CacheTier tier, CancellationToken ct = default) where T : class
        {
            _entries[key] = value;
            return Task.CompletedTask;
        }
    }
}
