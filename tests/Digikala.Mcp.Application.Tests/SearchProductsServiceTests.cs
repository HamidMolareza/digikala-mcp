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

        var result = await _service.ExecuteAsync("iphone");

        Assert.Same(cached, result);
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

        var result = await _service.ExecuteAsync("iphone");

        Assert.True(result.IsStale);
        Assert.NotNull(result.Warning);
        Assert.Equal(3, result.TotalCount);
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
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 500 for https://api.digikala.com/v1/categories/search/?page=1&q=iphone"));

        var result = await _service.ExecuteAsync("iphone");

        Assert.NotNull(result.Error);
        Assert.Equal("search_products", result.Error!.Tool);
        Assert.Equal(500, result.Error.StatusCode);
        Assert.True(result.Error.Retryable);
        Assert.Equal(0, result.FilteredCount);
        Assert.NotNull(result.Warning);
    }
}
