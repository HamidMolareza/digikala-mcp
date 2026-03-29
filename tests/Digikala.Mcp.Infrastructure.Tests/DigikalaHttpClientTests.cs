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

        var requestsBeforeFinalAttempt = requestCount;
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAutocompleteAsync("iphone"));

        Assert.Contains("Circuit breaker is open", exception.Message, StringComparison.Ordinal);
        Assert.Equal(requestsBeforeFinalAttempt, requestCount);
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
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":200,\"data\":{}}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        await client.SearchCategoryAsync("mobile-phone", query: "iphone");

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal(
            "https://api.example.com/v1/categories/mobile-phone/search/?page=1&q=iphone",
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
