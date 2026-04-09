using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class GetProductFeedbackSummaryServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnDeterministicThemes_AndAggregatedSignals()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());

        apiClient.GetProductV2Async(951998, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 951998,
                      "title_fa": "شامپو روزانه ملایم بدون سولفات برای موهای چرب",
                      "status": "marketable",
                      "url": { "uri": "/product/dkp-951998" },
                      "images": [],
                      "default_variant": { "price": { "selling_price": 1000 } },
                      "expert_reviews": {
                        "description": "فرمول ملایم و آبرسان",
                        "advantages": ["ملایم", "مناسب استفاده روزانه"],
                        "disadvantages": ["برای پوست خیلی چرب ممکن است خیلی ملایم باشد"]
                      },
                      "specifications": [
                        {
                          "title": "مشخصات",
                          "attributes": [
                            { "title": "ویژگی", "values": ["بدون سولفات"] }
                          ]
                        }
                      ]
                    }
                  }
                }
                """));

        apiClient.GetProductCommentsAsync(951998, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [
                      {
                        "id": 1,
                        "title": "ملایم",
                        "body": "مو را خشک نمی کند و برای مصرف روزانه مناسب است",
                        "rate": 5,
                        "created_at": "2026-03-29T00:00:00Z",
                        "reactions": { "likes": 8, "dislikes": 0 },
                        "advantages": ["خشک نمی کند"],
                        "disadvantages": []
                      },
                      {
                        "id": 2,
                        "title": "خیلی قوی نیست",
                        "body": "برای کف سر خیلی چرب شاید زیادی ملایم باشد",
                        "rate": 4,
                        "created_at": "2026-03-28T00:00:00Z",
                        "reactions": { "likes": 3, "dislikes": 0 },
                        "advantages": [],
                        "disadvantages": ["برای کف سر خیلی چرب زیادی ملایم است"]
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 }
                  }
                }
                """));

        var service = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var result = await service.ExecuteAsync(951998);

        Assert.Equal(951998, result.ProductId);
        Assert.Contains(result.Pros, item => item.Label.Contains("ملایم", StringComparison.Ordinal));
        Assert.Contains(result.Themes, item => item.Name == "mildness");
        Assert.Contains(result.Themes, item => item.Name == "daily_use");
        Assert.Contains("gentle", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseBuyerCommentsSummary_WhenNativeOverviewExists()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());

        apiClient.GetProductV2Async(20491421, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 20491421,
                      "title_fa": "پاوربانک",
                      "status": "marketable",
                      "url": { "uri": "/product/dkp-20491421" },
                      "images": [],
                      "default_variant": { "price": { "selling_price": 1000 } },
                      "comments_overview": {
                        "id": 433870,
                        "overview": "کیفیت ساخت بالا و شارژ سریع از نقاط قوت است، اما کابل کوتاه است.",
                        "advantages": ["کیفیت ساخت بالا", "شارژ سریع"],
                        "disadvantages": ["کابل کوتاه"]
                      }
                    }
                  }
                }
                """));

        apiClient.GetProductCommentsAsync(20491421, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 }
                  }
                }
                """));

        var service = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var result = await service.ExecuteAsync(20491421);

        Assert.Equal(20491421, result.ProductId);
        Assert.Contains(result.Pros, item => item.Label == "کیفیت ساخت بالا");
        Assert.Contains(result.Pros, item => item.Label == "شارژ سریع");
        Assert.Contains(result.Cons, item => item.Label == "کابل کوتاه");
        Assert.NotEmpty(result.Summary);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_ForInvalidInputs()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var service = new GetProductFeedbackSummaryService(detailService, reviewsService);

        var result = await service.ExecuteAsync(0, maxPros: 0, maxCons: 0, maxThemes: 0, maxSnippets: 0);

        Assert.NotNull(result.Error);
        Assert.Equal("InvalidInput", result.Error!.Code);
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`productId`", StringComparison.Ordinal));
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`maxPros`", StringComparison.Ordinal));
        await apiClient.DidNotReceiveWithAnyArgs().GetProductV2Async(default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldClassifyRateLimiterFailures_AsRetryableRateLimited()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var service = new GetProductFeedbackSummaryService(detailService, reviewsService);

        apiClient.GetProductV2Async(951998, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Client-side rate limiter rejected the request."));

        var result = await service.ExecuteAsync(951998);

        Assert.NotNull(result.Error);
        Assert.Equal("RateLimited", result.Error!.Code);
        Assert.True(result.Error.Retryable);
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
