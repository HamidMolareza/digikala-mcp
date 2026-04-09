using System.Net;
using System.Text.Json;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Configuration;
using Digikala.Mcp.Infrastructure.HttpClients;
using Digikala.Mcp.Infrastructure.Models.Common;
using Digikala.Mcp.Infrastructure.Models.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digikala.Mcp.Infrastructure.Tests;

[CollectionDefinition("Live Digikala API", DisableParallelization = true)]
public sealed class LiveDigikalaApiCollection : ICollectionFixture<LiveDigikalaApiFixture>;

[Collection("Live Digikala API")]
public sealed class LiveDigikalaContractTests(LiveDigikalaApiFixture fixture)
{
    [Fact]
    public async Task SearchCategoryAsync_ShouldDeserializeSearchEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.SearchCategoryAsync(
            fixture.CategorySlug,
            query: fixture.SearchQuery);

        var envelope = DeserializeEnvelope<SearchResponseData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.NotNull(envelope.Data!.Products);
        Assert.NotNull(envelope.Data.Pager);
        Assert.NotNull(envelope.Data.SortOptions);
        Assert.True(envelope.Data.Pager!.CurrentPage >= 1);
        Assert.True(envelope.Data.Pager.TotalPages >= 1);
    }

    [Fact]
    public async Task GetProductV2Async_ShouldDeserializeProductDetailEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.GetProductV2Async(fixture.ProductId);

        var envelope = DeserializeEnvelope<ProductDetailData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data?.Product);
        Assert.True(envelope.Data!.Product!.Id > 0);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.Product.TitleFa ?? envelope.Data.Product.TitleEn));
        Assert.NotNull(envelope.Data.Product.Url);
        Assert.NotNull(envelope.Data.Product.DefaultVariant);
    }

    [Fact]
    public async Task GetProductCommentsAsync_ShouldDeserializeCommentsEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.GetProductCommentsAsync(fixture.ProductId);

        var envelope = DeserializeEnvelope<CommentsResponseData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.NotNull(envelope.Data!.Comments);
        Assert.NotNull(envelope.Data.Pager);
        Assert.True(envelope.Data.Pager!.CurrentPage >= 1);
    }

    [Fact]
    public async Task GetProductQuestionsAsync_ShouldDeserializeQuestionsEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.GetProductQuestionsAsync(fixture.ProductId);

        var envelope = DeserializeEnvelope<QuestionsResponseData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.True(envelope.Data!.Questions is null || envelope.Data.Questions.Count >= 0);
        Assert.NotNull(envelope.Data.Pager);
        Assert.True(envelope.Data.Pager!.CurrentPage >= 1);
    }

    [Fact]
    public async Task GetProductPriceChartAsync_ShouldDeserializePriceChartEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.GetProductPriceChartAsync(fixture.ProductId);

        var envelope = DeserializeEnvelope<PriceChartResponseData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data!.Today));
        Assert.NotNull(envelope.Data.PriceChart);
        Assert.NotEmpty(envelope.Data.PriceChart!);
        Assert.NotNull(envelope.Data.PriceChart![0].History);
    }

    [Fact]
    public async Task GetBestSellingAsync_ShouldDeserializeBestSellingEnvelope()
    {
        if (!IsLiveTestsEnabled())
        {
            return;
        }

        using var response = await fixture.Client.GetBestSellingAsync();

        var envelope = DeserializeEnvelope<BestSellingData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.NotNull(envelope.Data!.Products);
        Assert.NotNull(envelope.Data.Categories);
    }

    private static DigikalaApiResponse<T> DeserializeEnvelope<T>(JsonDocument response)
    {
        var envelope = JsonSerializer.Deserialize<DigikalaApiResponse<T>>(response.RootElement.GetRawText());
        Assert.NotNull(envelope);
        return envelope!;
    }

    private static bool IsLiveTestsEnabled() => LiveDigikalaApiFixture.IsEnabled;
}

public sealed class LiveDigikalaApiFixture : IDisposable
{
    private readonly HttpClient _httpClient;

    public static bool IsEnabled => GetBooleanEnvironmentVariable("DIGIKALA_RUN_LIVE_CONTRACT_TESTS");

    public IDigikalaApiClient Client { get; }

    public long ProductId { get; } = GetInt64EnvironmentVariable("DIGIKALA_LIVE_PRODUCT_ID", 651040);

    public string CategorySlug { get; } = GetStringEnvironmentVariable("DIGIKALA_LIVE_CATEGORY_SLUG", "hair-shampoo");

    public string SearchQuery { get; } = GetStringEnvironmentVariable("DIGIKALA_LIVE_SEARCH_QUERY", "شامپو");

    public LiveDigikalaApiFixture()
    {
        var options = new DigikalaOptions
        {
            BaseUrl = GetStringEnvironmentVariable("DIGIKALA_LIVE_BASE_URL", "https://api.digikala.com"),
            FeaturesBaseUrl = GetStringEnvironmentVariable("DIGIKALA_LIVE_FEATURES_BASE_URL", "https://api-zakaria.digikala.com"),
            TimeoutSeconds = (int)GetInt64EnvironmentVariable("DIGIKALA_LIVE_TIMEOUT_SECONDS", 20),
            UserAgent = GetStringEnvironmentVariable(
                "DIGIKALA_LIVE_USER_AGENT",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36"),
        };

        _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
        };

        _httpClient.DefaultRequestHeaders.Add("X-Web-Optimize-Response", "1");
        _httpClient.DefaultRequestHeaders.Add("X-Web-Client", "desktop");
        _httpClient.DefaultRequestHeaders.Add("X-Web-Client-Id", "web");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://www.digikala.com");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.digikala.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);

        Client = new DigikalaHttpClient(
            _httpClient,
            Options.Create(options),
            NullLogger<DigikalaHttpClient>.Instance);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static bool GetBooleanEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value is not null &&
               (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase));
    }

    private static long GetInt64EnvironmentVariable(string name, long fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return long.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string GetStringEnvironmentVariable(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
