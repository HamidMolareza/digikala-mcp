using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class StructuredToolErrorHandlingTests
{
    private static readonly ICacheStore NoCache = new NullCacheStore();

    [Fact]
    public async Task AutocompleteService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetAutocompleteAsync("iphone", Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/autocomplete/?q=iphone"));

        var service = new AutocompleteService(apiClient, NoCache, new AutocompleteMapper());
        var result = await service.ExecuteAsync("iphone");

        AssertStructuredError(result.Error, "autocomplete", "UpstreamError", 503, "/v1/autocomplete/?q=iphone");
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task BrowseByTagService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetTagProductsAsync("iphone-16", 2, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/tags/iphone-16/search/?page=2"));

        var service = new BrowseByTagService(apiClient, NoCache, new ProductMapper());
        var result = await service.ExecuteAsync("iphone-16", page: 2);

        AssertStructuredError(result.Error, "browse_by_tag", "UpstreamError", 503, "/v1/tags/iphone-16/search/?page=2");
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task GetBrandPageService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetBrandPremiumAsync("persil", Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 404 for https://api.digikala.com/v1/brands/persil/"));

        var service = new GetBrandPageService(apiClient, NoCache, new BrandMapper(new ProductMapper()));
        var result = await service.ExecuteAsync("persil");

        AssertStructuredError(result.Error, "get_brand_page", "NotFound", 404, "/v1/brands/persil/");
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.BestSellingProducts);
        Assert.Empty(result.NewestProducts);
    }

    [Fact]
    public async Task GetCategoryLandingService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetCategoryLandingAsync("mobile-phone", Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/categories/mobile-phone/"));

        var service = new GetCategoryLandingService(apiClient, NoCache, new CategoryMapper(new ProductMapper()));
        var result = await service.ExecuteAsync("mobile-phone");

        AssertStructuredError(result.Error, "get_category_landing", "UpstreamError", 503, "/v1/categories/mobile-phone/");
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.SubCategories);
    }

    [Fact]
    public async Task GetIncredibleOffersService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetIncredibleOffersProductsAsync(3, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/incredible-offers/products/?page=3"));

        var service = new GetIncredibleOffersService(apiClient, NoCache, new OfferMapper());
        var result = await service.ExecuteAsync(page: 3);

        AssertStructuredError(result.Error, "get_incredible_offers", "UpstreamError", 503, "/v1/incredible-offers/products/?page=3");
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Offers);
    }

    [Fact]
    public async Task GetProductDetailService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductV2Async(1001, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 404 for https://api.digikala.com/v2/product/1001/"));

        var service = new GetProductDetailService(apiClient, NoCache, new ProductMapper());
        var result = await service.ExecuteAsync(1001);

        AssertStructuredError(result.Error, "get_product_detail", "NotFound", 404, "/v2/product/1001/");
        Assert.Equal(1001, result.Id);
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductPriceChartService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductPriceChartAsync(1001, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new TimeoutException("Timed out while calling https://api.digikala.com/v1/product/1001/price-chart/"));

        var service = new GetProductPriceChartService(apiClient, NoCache, new PriceChartMapper());
        var result = await service.ExecuteAsync(1001);

        AssertStructuredError(result.Error, "get_product_price_chart", "UpstreamTimeout", null, null);
        Assert.True(result.Error!.Retryable);
        Assert.Equal(1001, result.ProductId);
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductQuestionsService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductQuestionsAsync(1001, 2, null, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/product/1001/questions/?page=2"));

        var service = new GetProductQuestionsService(apiClient, NoCache, new QuestionMapper());
        var result = await service.ExecuteAsync(1001, page: 2);

        AssertStructuredError(result.Error, "get_product_questions", "UpstreamError", 503, "/v1/product/1001/questions/?page=2");
        Assert.Equal(2, result.CurrentPage);
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Questions);
    }

    [Fact]
    public async Task GetProductReviewsService_ShouldReturnStructuredError_WhenUpstreamFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductCommentsAsync(1001, 2, null, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/product/1001/comments/?page=2"));

        var service = new GetProductReviewsService(apiClient, NoCache, new ReviewMapper());
        var result = await service.ExecuteAsync(1001, page: 2);

        AssertStructuredError(result.Error, "get_product_reviews", "UpstreamError", 503, "/v1/product/1001/comments/?page=2");
        Assert.Equal(2, result.CurrentPage);
        Assert.Contains("structured diagnostics", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Reviews);
    }

    private static void AssertStructuredError(
        ToolIssueDto? error,
        string tool,
        string code,
        int? statusCode,
        string? upstreamEndpoint)
    {
        var issue = Assert.IsType<ToolIssueDto>(error);
        Assert.Equal(tool, issue.Tool);
        Assert.Equal(code, issue.Code);
        Assert.Equal(statusCode, issue.StatusCode);
        Assert.Equal(upstreamEndpoint, issue.UpstreamEndpoint);
        Assert.NotEmpty(issue.Diagnostics);
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
