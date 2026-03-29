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
        Assert.Equal(2, result.BestMatch!.Id);
        Assert.Contains("min_rating>=4.5", result.AppliedFilters);
        Assert.Contains("min_review_count>=500", result.AppliedFilters);
        Assert.All(result.Products, item => Assert.True(item.SearchScore > 0));
    }
}
