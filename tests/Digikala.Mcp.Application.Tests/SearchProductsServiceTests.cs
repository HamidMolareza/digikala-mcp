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
}
