using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class GetProductOverviewServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnRankedOverviewSections()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var priceChartService = new GetProductPriceChartService(apiClient, cache, new PriceChartMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var questionsService = new GetProductQuestionsService(apiClient, cache, new QuestionMapper());
        var recommendationsService = new GetProductRecommendationsService(apiClient, cache, new ProductMapper());

        apiClient.GetProductV2Async(1001, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 1001,
                      "title_fa": "Phone",
                      "status": "marketable",
                      "rating": { "rate": 4.8, "count": 1200 },
                      "default_variant": {
                        "price": { "selling_price": 100 },
                        "seller": { "title": "Seller A", "rating": 4.9 },
                        "warranty": { "title_fa": "Warranty" }
                      },
                      "url": { "uri": "/product/1001" },
                      "images": [],
                      "brand": { "title_fa": "Brand" },
                      "specifications": []
                    }
                  }
                }
                """));
        apiClient.GetProductPriceChartAsync(1001, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "today": { "selling_price": 100 },
                    "price_chart": [
                      { "date": "2026-03-27", "price": 95 },
                      { "date": "2026-03-28", "price": 100 }
                    ]
                  }
                }
                """));
        apiClient.GetProductCommentsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [
                      {
                        "id": 1,
                        "rate": 4,
                        "created_at": "2026-03-28T00:00:00Z",
                        "reactions": { "likes": 2, "dislikes": 0 }
                      },
                      {
                        "id": 2,
                        "rate": 5,
                        "created_at": "2026-03-29T00:00:00Z",
                        "reactions": { "likes": 10, "dislikes": 0 }
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 }
                  }
                }
                """));
        apiClient.GetProductQuestionsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "questions": [
                      {
                        "id": 1,
                        "created_at": "2026-03-28T00:00:00Z",
                        "answers": [
                          { "id": 1, "reactions": { "likes": 1 } }
                        ]
                      },
                      {
                        "id": 2,
                        "created_at": "2026-03-29T00:00:00Z",
                        "answers": [
                          { "id": 2, "reactions": { "likes": 4 } },
                          { "id": 3, "reactions": { "likes": 3 } }
                        ]
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 2 }
                  }
                }
                """));
        apiClient.GetProductRecommendationAsync(1001, 0, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "products": [
                      {
                        "id": 7,
                        "title_fa": "Rec A",
                        "status": "marketable",
                        "default_variant": { "price": { "selling_price": 200 } },
                        "url": { "uri": "/product/7" },
                        "images": []
                      },
                      {
                        "id": 8,
                        "title_fa": "Rec B",
                        "status": "marketable",
                        "default_variant": { "price": { "selling_price": 300 } },
                        "url": { "uri": "/product/8" },
                        "images": []
                      }
                    ]
                  }
                }
                """));

        var service = new GetProductOverviewService(
            detailService,
            priceChartService,
            reviewsService,
            questionsService,
            recommendationsService);

        var result = await service.ExecuteAsync(1001, topReviewsCount: 1, topQuestionsCount: 1, topRecommendationsCount: 1);

        Assert.Equal(1001, result.Detail.Id);
        Assert.Equal(2, result.TopReviews.Single().Id);
        Assert.Equal(2, result.TopQuestions.Single().Id);
        Assert.Equal(7, result.Recommendations.Single().Id);
        Assert.Equal("ok", result.SectionStatus["detail"]);
        Assert.Empty(result.SectionIssues);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPartialOverview_WhenOptionalSectionsFail()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var priceChartService = new GetProductPriceChartService(apiClient, cache, new PriceChartMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var questionsService = new GetProductQuestionsService(apiClient, cache, new QuestionMapper());
        var recommendationsService = new GetProductRecommendationsService(apiClient, cache, new ProductMapper());

        apiClient.GetProductV2Async(1001, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 1001,
                      "title_fa": "Phone",
                      "status": "marketable",
                      "default_variant": { "price": { "selling_price": 100 } },
                      "url": { "uri": "/product/1001" },
                      "images": []
                    }
                  }
                }
                """));
        apiClient.GetProductPriceChartAsync(1001, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 503 for https://api.digikala.com/v1/product/1001/price-chart/"));
        apiClient.GetProductCommentsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "comments": [], "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 } } }"""));
        apiClient.GetProductQuestionsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "questions": [], "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 } } }"""));
        apiClient.GetProductRecommendationAsync(1001, 0, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 500 for https://api.digikala.com/v1/product/1001/recommendation/?offset=0"));

        var service = new GetProductOverviewService(
            detailService,
            priceChartService,
            reviewsService,
            questionsService,
            recommendationsService);

        var result = await service.ExecuteAsync(1001);

        Assert.Equal(1001, result.Detail.Id);
        Assert.False(result.HasError);
        Assert.Equal("ok", result.SectionStatus["detail"]);
        Assert.Equal("failed", result.SectionStatus["priceChart"]);
        Assert.Equal("failed", result.SectionStatus["recommendations"]);
        Assert.NotEmpty(result.SectionWarnings);
        Assert.Contains("priceChart", result.SectionIssues.Keys);
        Assert.Contains("recommendations", result.SectionIssues.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStructuredDetailFailure_WhenMainDetailFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var detailService = new GetProductDetailService(apiClient, cache, new ProductMapper());
        var priceChartService = new GetProductPriceChartService(apiClient, cache, new PriceChartMapper());
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var questionsService = new GetProductQuestionsService(apiClient, cache, new QuestionMapper());
        var recommendationsService = new GetProductRecommendationsService(apiClient, cache, new ProductMapper());

        apiClient.GetProductV2Async(1001, null, Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 404 for https://api.digikala.com/v2/product/1001/"));
        apiClient.GetProductPriceChartAsync(1001, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "today": { "selling_price": 100 }, "price_chart": [] } }"""));
        apiClient.GetProductCommentsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "comments": [], "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 } } }"""));
        apiClient.GetProductQuestionsAsync(1001, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "questions": [], "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 } } }"""));
        apiClient.GetProductRecommendationAsync(1001, 0, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""{ "data": { "products": [] } }"""));

        var service = new GetProductOverviewService(
            detailService,
            priceChartService,
            reviewsService,
            questionsService,
            recommendationsService);

        var result = await service.ExecuteAsync(1001);

        Assert.True(result.HasError);
        Assert.Equal("failed", result.SectionStatus["detail"]);
        Assert.Contains("detail", result.SectionIssues.Keys);
        Assert.NotNull(result.Detail.Warning);
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
