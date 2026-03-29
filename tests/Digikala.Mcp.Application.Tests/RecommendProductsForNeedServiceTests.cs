using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class RecommendProductsForNeedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRecommendBalancedCandidate_WithTransparentEvidence()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = new SearchProductsService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "status": 200,
                  "data": {
                    "products": [
                      {
                        "id": 100,
                        "title_fa": "شامپو کنترل کننده چربی قوی",
                        "status": "marketable",
                        "rating": { "rate": 4.7, "count": 1600 },
                        "default_variant": { "price": { "selling_price": 2200000 } },
                        "url": { "uri": "/product/dkp-100" },
                        "images": []
                      },
                      {
                        "id": 200,
                        "title_fa": "شامپو روزانه ملایم بدون سولفات برای موهای چرب",
                        "status": "marketable",
                        "rating": { "rate": 4.6, "count": 1400 },
                        "default_variant": { "price": { "selling_price": 2400000 } },
                        "url": { "uri": "/product/dkp-200" },
                        "images": []
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 },
                    "filters": [],
                    "sort_options": []
                  }
                }
                """));

        apiClient.GetProductV2Async(100, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 100,
                      "title_fa": "شامپو کنترل کننده چربی قوی",
                      "status": "marketable",
                      "url": { "uri": "/product/dkp-100" },
                      "images": [],
                      "default_variant": { "price": { "selling_price": 2200000 } },
                      "expert_reviews": {
                        "description": "فرمول قوی برای کنترل چربی",
                        "advantages": ["کنترل چربی بالا"],
                        "disadvantages": ["ممکن است مو را خشک کند"]
                      }
                    }
                  }
                }
                """));

        apiClient.GetProductV2Async(200, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 200,
                      "title_fa": "شامپو روزانه ملایم بدون سولفات برای موهای چرب",
                      "status": "marketable",
                      "url": { "uri": "/product/dkp-200" },
                      "images": [],
                      "default_variant": { "price": { "selling_price": 2400000 } },
                      "expert_reviews": {
                        "description": "فرمول ملایم و متعادل بدون سولفات",
                        "advantages": ["مناسب استفاده روزانه", "خشک نمی کند"],
                        "disadvantages": ["برای کف سر خیلی چرب ممکن است کمی ملایم باشد"]
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

        apiClient.GetProductCommentsAsync(100, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [
                      {
                        "id": 1,
                        "title": "خیلی قوی",
                        "body": "برای موهای چرب خوب است ولی کمی خشک می کند",
                        "rate": 4,
                        "created_at": "2026-03-29T00:00:00Z",
                        "reactions": { "likes": 4, "dislikes": 0 },
                        "advantages": ["چربی را خوب کم می کند"],
                        "disadvantages": ["خشک می کند"]
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 }
                  }
                }
                """));

        apiClient.GetProductCommentsAsync(200, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [
                      {
                        "id": 2,
                        "title": "متعادل",
                        "body": "برای مصرف روزانه خوب است و مو را خشک نمی کند",
                        "rate": 5,
                        "created_at": "2026-03-29T00:00:00Z",
                        "reactions": { "likes": 9, "dislikes": 0 },
                        "advantages": ["ملایم", "خشک نمی کند"],
                        "disadvantages": []
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 }
                  }
                }
                """));

        var result = await service.ExecuteAsync(
            "Find me a shampoo for oily hair that is not too strong and does not over-dry the hair.",
            limit: 1,
            onlyAvailable: true,
            minRating: 4.0,
            minReviewCount: 30,
            includeEvidence: true);

        Assert.Equal("shampoo oily hair", result.SearchQuery);
        Assert.Contains("mild", result.QueryUnderstanding.PreferredSignals);
        Assert.Contains("non-drying", result.QueryUnderstanding.PreferredSignals);
        Assert.Single(result.Recommendations);
        Assert.Equal(200, result.Recommendations[0].Product.Id);
        Assert.Contains(result.Recommendations[0].Why, item => item.Contains("milder", StringComparison.OrdinalIgnoreCase) || item.Contains("gentle", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(result.Recommendations[0].Evidence);
        Assert.Contains(result.Recommendations[0].Evidence!.ReviewSignals, item => item.StartsWith("mildness:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStructuredSearchError_WhenSearchFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = new SearchProductsService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchCategoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<Dictionary<string, string[]>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 500 for https://api.digikala.com/v1/categories/search/?page=1&q=shampoo"));

        var result = await service.ExecuteAsync("Need a balanced shampoo for oily hair.", limit: 2);

        Assert.NotNull(result.Error);
        Assert.Equal("search_products", result.Error!.Tool);
        Assert.Empty(result.Recommendations);
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
