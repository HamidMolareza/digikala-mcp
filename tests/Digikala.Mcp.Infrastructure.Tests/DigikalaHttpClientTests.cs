using System.Net;
using System.Text;
using Digikala.Mcp.Infrastructure.Configuration;
using Digikala.Mcp.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digikala.Mcp.Infrastructure.Tests;

public class DigikalaHttpClientTests
{
    [Fact]
    public async Task GetFeatureFlagsAsync_ShouldUseFeatureHost()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":200,\"data\":{}}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        await client.GetFeatureFlagsAsync("sdk-key");

        Assert.NotNull(handler.LastRequestUri);
        Assert.StartsWith("https://features.example.com/api/features/sdk-key", handler.LastRequestUri!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSuperAppPillarsAsync_ShouldUseAnonymousSuperAppRoute()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetSuperAppPillarsAsync();

        Assert.Equal("https://api.example.com/v1/super-app/pillars/", handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task RepeatedFailures_ShouldOpenCircuit()
    {
        var requestCount = 0;
        var handler = new RecordingHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"status\":500}", Encoding.UTF8, "application/json"),
            };
        });
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAutocompleteAsync("iphone"));
        }

        Assert.Equal(5, requestCount);

        var requestsBeforeFinalAttempt = requestCount;
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAutocompleteAsync("iphone"));

        Assert.Contains("Circuit breaker is open", exception.Message, StringComparison.Ordinal);
        Assert.Equal(requestsBeforeFinalAttempt, requestCount);
    }

    [Fact]
    public async Task RepeatedNotFoundResponses_ShouldNotOpenCircuit()
    {
        var requestCount = 0;
        var handler = new RecordingHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"status\":404}", Encoding.UTF8, "application/json"),
            };
        });
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        for (var i = 0; i < 6; i++)
        {
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAutocompleteAsync("powerbank"));
            Assert.DoesNotContain("Circuit breaker is open", exception.Message, StringComparison.Ordinal);
        }

        Assert.Equal(6, requestCount);
    }

    [Fact]
    public async Task SearchCategoryAsync_ShouldRejectGlobalSearchPlaceholder_WhenSlugIsSearch()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":200,\"data\":{}}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => client.SearchCategoryAsync("search", query: "iphone"));

        Assert.Contains("requires a real category slug", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequestUri);
    }

    [Fact]
    public async Task SearchCategoryAsync_ShouldUseCategorySearchRoute_ForRealCategory()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.SearchCategoryAsync("mobile-phone", query: "iphone");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal(
            "https://api.example.com/v1/categories/mobile-phone/search/?page=1&q=iphone",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetHomepageV2Async_ShouldUseHomepageRoute()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetHomepageV2Async();

        Assert.Equal("https://api.example.com/v2/", handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetProvidersProductsAsync_ShouldIncludeCategoryPageAndQuery()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetProvidersProductsAsync("mobile-phone", page: 3, query: "iphone");

        Assert.Equal(
            "https://api.example.com/v1/providers-products/?category_code=mobile-phone&page=3&q=iphone",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetHomeRecommendationAsync_ShouldIncludeWebPageParameter()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetHomeRecommendationAsync("superapp");

        Assert.Equal(
            "https://api.example.com/v1/recommendation/?web_page=superapp",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetPersonalizedCategoryRecommendationAsync_ShouldUseCategoryPathAndPage()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetPersonalizedCategoryRecommendationAsync("vitamins", page: 2);

        Assert.Equal(
            "https://api.example.com/v1/recommendation/personalized-category/vitamins/?page=2",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetDynamicLandingMobileAsync_ShouldUseDynamicLandingRoute()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetDynamicLandingMobileAsync();

        Assert.Equal(
            "https://api.example.com/v1/dynamic-landing/mobile/",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetDynamicLandingWidgetAsync_ShouldUseWidgetRoute()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetDynamicLandingWidgetAsync(9491);

        Assert.Equal(
            "https://api.example.com/v1/dynamic-landing/widget/9491/",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetMagnetHomePageAsync_ShouldUseMagnetHomePageRoute()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetMagnetHomePageAsync();

        Assert.Equal(
            "https://api.example.com/v1/magnet/home-page/",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetAnnouncementMessageAsync_ShouldIncludeClientPage()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetAnnouncementMessageAsync("super-app");

        Assert.Equal(
            "https://api.example.com/v1/magnet/notification-center/announcement/message/?clientPage=super-app",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetRelatedPostsAsync_ShouldIncludeProductIdAndWarehouse()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetRelatedPostsAsync(7158804, warehouseId: 29);

        Assert.Equal(
            "https://api.example.com/v1/magnet/related-posts/7158804/?_whid=29&productId=7158804",
            handler.LastRequestUri!.ToString());
    }

    [Fact]
    public async Task GetCommentsOverviewUserFeedbackAsync_ShouldIncludeOverviewIdInPathAndQuery()
    {
        var handler = CreateSuccessHandler();
        var client = CreateClient(new HttpClient(handler));

        await client.GetCommentsOverviewUserFeedbackAsync(20112);

        Assert.Equal(
            "https://api.example.com/v1/comments-overview/20112/user-feedback/?overviewId=20112",
            handler.LastRequestUri!.ToString());
    }

    private static DigikalaHttpClient CreateClient(HttpClient httpClient)
    {
        var options = Options.Create(new DigikalaOptions
        {
            BaseUrl = "https://api.example.com",
            FeaturesBaseUrl = "https://features.example.com",
        });

        return new DigikalaHttpClient(httpClient, options, NullLogger<DigikalaHttpClient>.Instance);
    }

    private static RecordingHandler CreateSuccessHandler() =>
        new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":200,\"data\":{}}", Encoding.UTF8, "application/json"),
        });

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(responder(request));
        }
    }
}
