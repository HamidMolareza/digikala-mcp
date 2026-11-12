using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Domain.ValueObjects;
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
        var searchService = CreateSearchService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>())
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
            categorySlug: "hair-shampoo",
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
    public async Task ExecuteAsync_ShouldBuildCompactCableSearch_ForRedmiUsbCNeeds()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = CreateSearchService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "status": 200,
                  "data": {
                    "products": [
                      {
                        "id": 300,
                        "title_fa": "کابل تبدیل USB به USB-C ریمکس مدل RC-C099 66W",
                        "status": "marketable",
                        "rating": { "rate": 4.5, "count": 320 },
                        "default_variant": { "price": { "selling_price": 3950000 } },
                        "url": { "uri": "/product/dkp-300" },
                        "images": []
                      }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 },
                    "filters": [],
                    "sort_options": []
                  }
                }
                """));

        apiClient.GetProductV2Async(300, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "product": {
                      "id": 300,
                      "title_fa": "کابل تبدیل USB به USB-C ریمکس مدل RC-C099 66W",
                      "status": "marketable",
                      "url": { "uri": "/product/dkp-300" },
                      "images": [],
                      "default_variant": { "price": { "selling_price": 3950000 } },
                      "expert_reviews": {
                        "description": "کابل مقاوم مناسب شارژ سریع",
                        "advantages": ["شارژ سریع", "کیفیت ساخت خوب"],
                        "disadvantages": []
                      },
                      "specifications": [
                        {
                          "title": "مشخصات",
                          "attributes": [
                            { "title": "نوع کابل", "values": ["USB Type-C", "USB"] }
                          ]
                        }
                      ]
                    }
                  }
                }
                """));

        apiClient.GetProductCommentsAsync(300, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "data": {
                    "comments": [],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 }
                  }
                }
                """));

        var result = await service.ExecuteAsync(
            "دنبال یه کابل خوب شارژ برای گوشی Redmi Note 10 میگردم که USB-C باشد، برای شارژ سریع مناسب باشد و دوام خوبی داشته باشد.",
            limit: 1,
            minRating: 4.0,
            minReviewCount: 20,
            includeEvidence: false);

        Assert.Equal("کابل شارژ USB به USB-C", result.SearchQuery);
        Assert.Contains("USB به USB-C", result.QueryUnderstanding.PreferredSignals);
        Assert.Contains("شارژ سریع", result.QueryUnderstanding.PreferredSignals);
        Assert.Contains("بادوام", result.QueryUnderstanding.PreferredSignals);
        Assert.Contains("lightning", result.QueryUnderstanding.AvoidSignals);
        Assert.Single(result.Recommendations);
        Assert.Equal(300, result.Recommendations[0].Product.Id);
        await apiClient.Received(1).SearchProductsAsync(Arg.Is<ProductSearchRequest>(r => r.CategorySlug == "cell-phone-data-cable" && r.Query == "کابل شارژ USB به USB-C"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStructuredSearchError_WhenSearchFails()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = CreateSearchService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonDocument>>(_ => throw new HttpRequestException("Digikala API returned HTTP 500 for https://api.digikala.com/v1/categories/hair-shampoo/search/?page=1&q=shampoo"));

        var result = await service.ExecuteAsync("Need a balanced shampoo for oily hair.", categorySlug: "hair-shampoo", limit: 2);

        Assert.NotNull(result.Error);
        Assert.Equal("search_products", result.Error!.Tool);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLimitFeedbackFanOut_ToTopShortlistCandidates()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = CreateSearchService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        apiClient.SearchProductsAsync(Arg.Any<ProductSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(JsonDocument.Parse("""
                {
                  "status": 200,
                  "data": {
                    "products": [
                      { "id": 101, "title_fa": "شامپو 101", "status": "marketable", "rating": { "rate": 4.9, "count": 1000 }, "default_variant": { "price": { "selling_price": 1000 } }, "url": { "uri": "/product/101" }, "images": [] },
                      { "id": 102, "title_fa": "شامپو 102", "status": "marketable", "rating": { "rate": 4.8, "count": 900 }, "default_variant": { "price": { "selling_price": 1100 } }, "url": { "uri": "/product/102" }, "images": [] },
                      { "id": 103, "title_fa": "شامپو 103", "status": "marketable", "rating": { "rate": 4.7, "count": 800 }, "default_variant": { "price": { "selling_price": 1200 } }, "url": { "uri": "/product/103" }, "images": [] },
                      { "id": 104, "title_fa": "شامپو 104", "status": "marketable", "rating": { "rate": 4.6, "count": 700 }, "default_variant": { "price": { "selling_price": 1300 } }, "url": { "uri": "/product/104" }, "images": [] },
                      { "id": 105, "title_fa": "شامپو 105", "status": "marketable", "rating": { "rate": 4.5, "count": 600 }, "default_variant": { "price": { "selling_price": 1400 } }, "url": { "uri": "/product/105" }, "images": [] }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 5 },
                    "filters": [],
                    "sort_options": []
                  }
                }
                """));

        foreach (var productId in new long[] { 101, 102, 103 })
        {
            apiClient.GetProductV2Async(productId, null, Arg.Any<CancellationToken>())
                .Returns(JsonDocument.Parse($$"""
                    {
                      "data": {
                        "product": {
                          "id": {{productId}},
                          "title_fa": "شامپو {{productId}}",
                          "status": "marketable",
                          "url": { "uri": "/product/{{productId}}" },
                          "images": [],
                          "default_variant": { "price": { "selling_price": 1000 } },
                          "expert_reviews": {
                            "description": "فرمول ملایم برای مصرف روزانه",
                            "advantages": ["ملایم"],
                            "disadvantages": []
                          }
                        }
                      }
                    }
                    """));

            apiClient.GetProductCommentsAsync(productId, 1, null, null, Arg.Any<CancellationToken>())
                .Returns(JsonDocument.Parse($$"""
                    {
                      "data": {
                        "comments": [
                          {
                            "id": {{productId}},
                            "title": "نظر",
                            "body": "برای استفاده روزانه مناسب است",
                            "rate": 5,
                            "created_at": "2026-03-29T00:00:00Z",
                            "reactions": { "likes": 5, "dislikes": 0 },
                            "advantages": ["ملایم"],
                            "disadvantages": []
                          }
                        ],
                        "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 }
                      }
                    }
                    """));
        }

        var result = await service.ExecuteAsync(
            "Need a gentle shampoo for oily hair.",
            categorySlug: "hair-shampoo",
            limit: 1,
            includeEvidence: true);

        Assert.Single(result.Recommendations);
        Assert.Contains("top 3 ranked search matches", result.Warning, StringComparison.OrdinalIgnoreCase);
        await apiClient.Received(1).GetProductV2Async(101, null, Arg.Any<CancellationToken>());
        await apiClient.Received(1).GetProductV2Async(102, null, Arg.Any<CancellationToken>());
        await apiClient.Received(1).GetProductV2Async(103, null, Arg.Any<CancellationToken>());
        await apiClient.DidNotReceive().GetProductV2Async(104, null, Arg.Any<CancellationToken>());
        await apiClient.DidNotReceive().GetProductV2Async(105, null, Arg.Any<CancellationToken>());
        await apiClient.DidNotReceive().GetProductCommentsAsync(104, 1, null, null, Arg.Any<CancellationToken>());
        await apiClient.DidNotReceive().GetProductCommentsAsync(105, 1, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationError_ForInvalidRecommendationInputs()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var reviewMapper = new ReviewMapper();
        var searchService = CreateSearchService(apiClient, cache, productMapper);
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, reviewMapper);
        var feedbackService = new GetProductFeedbackSummaryService(detailService, reviewsService);
        var service = new RecommendProductsForNeedService(searchService, feedbackService);

        var result = await service.ExecuteAsync(
            string.Empty,
            categorySlug: "hair-shampoo",
            limit: 0,
            minPriceRials: 2_000,
            maxPriceRials: 1_000);

        Assert.NotNull(result.Error);
        Assert.Equal("InvalidInput", result.Error!.Code);
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`needDescription`", StringComparison.Ordinal));
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`limit`", StringComparison.Ordinal));
        Assert.Contains(result.Error.Diagnostics, item => item.Contains("`minPriceRials` cannot be greater", StringComparison.Ordinal));
        Assert.Empty(result.Recommendations);
        await apiClient.DidNotReceiveWithAnyArgs().SearchProductsAsync(default!, default);
    }

    private static SearchProductsService CreateSearchService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper productMapper)
    {
        var autocompleteService = new AutocompleteService(apiClient, cache, new AutocompleteMapper());
        var categoryResolutionService = new CategoryResolutionService(autocompleteService);
        return new SearchProductsService(apiClient, cache, productMapper, categoryResolutionService);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSearchProductPhrase_AndRetryBroader_WhenLongNeedFindsNothing()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        var cache = new NullCacheStore();
        var productMapper = new ProductMapper();
        var detailService = new GetProductDetailService(apiClient, cache, productMapper);
        var reviewsService = new GetProductReviewsService(apiClient, cache, new ReviewMapper());
        var service = new RecommendProductsForNeedService(
            CreateSearchService(apiClient, cache, productMapper),
            new GetProductFeedbackSummaryService(detailService, reviewsService));
        apiClient.GetAutocompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => JsonDocument.Parse("""{ "status": 200, "data": { "auto_complete": [] } }"""));
        apiClient.SearchProductsAsync(Arg.Is<ProductSearchRequest>(r => r.Query == "ماگ سرامیکی بزرگ"), Arg.Any<CancellationToken>())
            .Returns(_ => JsonDocument.Parse("""{ "status": 200, "data": { "products": [], "pager": { "current_page": 1, "total_pages": 1, "total_items": 0 } } }"""));
        apiClient.SearchProductsAsync(Arg.Is<ProductSearchRequest>(r => r.Query == "ماگ سرامیکی"), Arg.Any<CancellationToken>())
            .Returns(_ => JsonDocument.Parse("""
                {
                  "status": 200,
                  "data": {
                    "products": [
                      { "id": 5, "title_fa": "ماگ سرامیکی", "status": "marketable", "rating": { "rate": 85, "count": 300 }, "default_variant": { "price": { "selling_price": 3000000 } }, "url": { "uri": "/product/dkp-5/" }, "images": [] }
                    ],
                    "pager": { "current_page": 1, "total_pages": 1, "total_items": 1 }
                  }
                }
                """));

        var result = await service.ExecuteAsync(
            "ماگ سرامیکی بزرگ با حجم بیش از 350 میلی لیتر، دسته خوش‌دست",
            limit: 1,
            includeEvidence: false);

        Assert.Equal("ماگ سرامیکی بزرگ", result.SearchQuery);
        Assert.Equal(5, Assert.Single(result.Recommendations).Product.Id);
        Assert.Contains("broader query `ماگ سرامیکی`", result.Warning, StringComparison.Ordinal);
        await apiClient.Received(1).SearchProductsAsync(
            Arg.Is<ProductSearchRequest>(r => r.Query == "ماگ سرامیکی بزرگ" && r.Sort == null && r.CategorySlug == null),
            Arg.Any<CancellationToken>());
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
