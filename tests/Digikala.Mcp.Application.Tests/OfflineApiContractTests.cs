using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class OfflineApiContractTests
{
    private static readonly ICacheStore NoCache = new NullCacheStore();
    private readonly ProductMapper _productMapper = new();
    private readonly AutocompleteMapper _autocompleteMapper = new();
    private readonly OfferMapper _offerMapper = new();
    private readonly QuestionMapper _questionMapper = new();
    private readonly ReviewMapper _reviewMapper = new();
    private readonly PriceChartMapper _priceChartMapper = new();
    private readonly CategoryMapper _categoryMapper;
    private readonly BrandMapper _brandMapper;

    public OfflineApiContractTests()
    {
        _categoryMapper = new CategoryMapper(_productMapper);
        _brandMapper = new BrandMapper(_productMapper);
    }

    [Fact]
    public void AutocompleteFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("autocomplete.json");

        var result = _autocompleteMapper.MapToAutocomplete(response);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal("شامپو ضد شوره", suggestion.Text);
        Assert.Equal("hair-shampoo", suggestion.CategorySlug);
        Assert.Equal("/search/?q=%D8%B4%D8%A7%D9%85%D9%BE%D9%88+%D8%B6%D8%AF+%D8%B4%D9%88%D8%B1%D9%87", suggestion.Url);
        Assert.Contains("هفت سین", result.TrendingSearches);
    }

    [Fact]
    public void SearchCategoryFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("search-category.json");

        var result = _productMapper.MapToSearchResult(response);

        var product = Assert.Single(result.Products);
        Assert.Equal(8511704, product.Id);
        Assert.Equal("روغن مایع سرخ کردنی بهار الماس - 900 میلی لیتر", product.Title);
        Assert.Equal(1890000, product.PriceRials);
        Assert.Equal(10, product.DiscountPercent);
        Assert.Equal("oil", product.CategorySlug);
        Assert.Equal("https://www.digikala.com/product/dkp-8511704", product.AbsoluteUrl);
        Assert.Equal(806, result.TotalCount);
        Assert.Equal(41, result.TotalPages);
        var filter = Assert.Single(result.AvailableFilters);
        Assert.Equal("brand", filter.Code);
        Assert.Equal("بهار", Assert.Single(filter.Options).Title);
        Assert.Contains("مرتبط\u200cترین", result.AvailableSortOptions);
    }

    [Fact]
    public async Task BrowseByTagFixture_ShouldMatchExpectedContract()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetTagProductsAsync("football-boots", 1, Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("browse-by-tag.json"));
        var service = new BrowseByTagService(apiClient, NoCache, _productMapper);

        var result = await service.ExecuteAsync("football-boots");

        var product = Assert.Single(result.Products);
        Assert.Equal(3001001, product.Id);
        Assert.Equal("کفش فوتبال مردانه", product.Title);
        Assert.Equal("football-boots", product.CategorySlug);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void ProductDetailFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("product-detail.json");
        var data = response.RootElement.GetProperty("data").GetProperty("product");

        var result = _productMapper.MapToDetail(data);

        Assert.Equal(651040, result.Id);
        Assert.Equal("سوپ نیمه آماده ورمیشل و مرغ هاتی کارا مقدار 70 گرم", result.Title);
        Assert.Equal("hoti-kara", result.BrandSlug);
        Assert.Equal("سوپرمارکت دیجی\u200cکالا", result.Seller);
        Assert.Equal("تضمین تازگی", result.Warranty);
        Assert.Equal(760000, result.PriceRials);
        Assert.Equal("70 گرم", Assert.Single(result.Specifications, item => item.Title == "وزن").Value);
        Assert.Equal(1001, Assert.Single(result.Variants).Id);
        Assert.Equal("آماده سازی سریع", Assert.Single(result.ProsCons!.Pros));
        Assert.Equal("نمک بالا", Assert.Single(result.ProsCons.Cons));
    }

    [Fact]
    public void ProductCommentsFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("product-comments.json");

        var result = _reviewMapper.MapToReviewList(response);

        var review = Assert.Single(result.Reviews);
        Assert.Equal(5001, review.Id);
        Assert.Equal("خوب بود", review.Title);
        Assert.Equal(4, review.Rating);
        Assert.True(review.IsVerifiedPurchase);
        Assert.Equal("طعم خوب", Assert.Single(review.Pros));
        Assert.Equal("بسته بندی کوچک", Assert.Single(review.Cons));
        Assert.Equal(4.1, result.AverageRating);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public void ProductQuestionsFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("product-questions.json");

        var result = _questionMapper.MapToQuestionList(response);

        var question = Assert.Single(result.Questions);
        Assert.Equal(6001, question.Id);
        Assert.Equal("برای چند نفر کافی است؟", question.Body);
        var answer = Assert.Single(question.Answers);
        Assert.Equal(7001, answer.Id);
        Assert.Equal("برای یک نفر", answer.Body);
        Assert.Equal(3, answer.LikeCount);
        Assert.Equal(14, result.TotalCount);
    }

    [Fact]
    public void ProductPriceChartFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("product-price-chart.json");

        var result = _priceChartMapper.MapToPriceChart(651040, response);

        Assert.Equal(651040, result.ProductId);
        Assert.Equal(760000, result.CurrentPriceRials);
        Assert.Equal(760000, result.LowestPriceRials);
        Assert.Equal(790000, result.HighestPriceRials);
        Assert.Equal(3, result.History.Count);
        Assert.Equal(new DateOnly(2026, 3, 27), result.History[^1].Date);
    }

    [Fact]
    public async Task ProductRecommendationsFixture_ShouldMatchExpectedContract()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductRecommendationAsync(425402, 0, Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("product-recommendation.json"));
        var service = new GetProductRecommendationsService(apiClient, NoCache, _productMapper);

        var result = await service.ExecuteAsync(425402);

        var product = Assert.Single(result);
        Assert.Equal(91001, product.Id);
        Assert.Equal("شامپو روزانه", product.Title);
        Assert.Equal(2200000, product.PriceRials);
    }

    [Fact]
    public async Task TrendingRecommendationFixture_ShouldMatchExpectedContract()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetTrendingRecommendationAsync(Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("trending-recommendation.json"));
        var service = new GetTrendingProductsService(apiClient, NoCache, _productMapper);

        var result = await service.ExecuteAsync();

        var product = Assert.Single(result);
        Assert.Equal(92001, product.Id);
        Assert.Equal("گوشی موبایل سامسونگ", product.Title);
        Assert.True(product.IsAvailable);
    }

    [Fact]
    public async Task BestSellingFixture_ShouldMatchExpectedContract()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetBestSellingAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("best-selling.json"));
        var service = new GetBestSellingService(apiClient, NoCache, _productMapper);

        var result = await service.ExecuteAsync();

        var product = Assert.Single(result);
        Assert.Equal(460390, product.Id);
        Assert.Equal("کنسرو ماهی تن در روغن گالکسی - 180 گرم", product.Title);
        Assert.Equal(1250000, product.PriceRials);
    }

    [Fact]
    public void CategoryLandingFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("category-landing.json");

        var result = _categoryMapper.MapToCategory(response);

        Assert.Equal(6518, result.Id);
        Assert.Equal("شامپو مو", result.Title);
        Assert.Equal("hair-shampoo", result.Slug);
        Assert.Equal("شامپو ضد شوره", Assert.Single(result.BestSellingProducts).Title);
        Assert.Equal("کلیر", Assert.Single(result.PopularBrands).Title);
        Assert.Equal("ضد شوره", Assert.Single(result.SubCategories).Title);
    }

    [Fact]
    public void BrandPremiumFixture_ShouldMatchExpectedContract()
    {
        using var response = LoadFixture("brand-premium.json");

        var result = _brandMapper.MapToBrandPage(response);

        Assert.Equal(2449, result.Id);
        Assert.Equal("پرسیل", result.Title);
        Assert.Equal("persil", result.Slug);
        Assert.Equal("https://example.com/persil.png", result.LogoUrl);
        Assert.Equal("پودر ماشین لباسشویی پرسیل", Assert.Single(result.NewestProducts).Title);
        Assert.Equal("مایع لباسشویی پرسیل", Assert.Single(result.BestSellingProducts).Title);
    }

    [Fact]
    public void IncredibleOffersFixture_ShouldMergeOfferBuckets()
    {
        using var response = LoadFixture("incredible-offers.json");

        var result = _offerMapper.MapToOffersList(response);

        Assert.Equal(2, result.Offers.Count);
        Assert.Contains(result.Offers, item => item.ProductId == 70001 && item.DiscountPercent == 15);
        Assert.Contains(result.Offers, item => item.ProductId == 70002 && item.DiscountPercent == 20);
    }

    [Fact]
    public async Task PagedIncredibleOffersFixture_ShouldUseProductsArray()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetIncredibleOffersProductsAsync(2, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("incredible-offers-products.json"));
        var service = new GetIncredibleOffersService(apiClient, NoCache, _offerMapper);

        var result = await service.ExecuteAsync(page: 2);

        var offer = Assert.Single(result.Offers);
        Assert.Equal(71001, offer.ProductId);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public async Task SellerVouchersFixture_ShouldMatchExpectedContract()
    {
        var apiClient = Substitute.For<IDigikalaApiClient>();
        apiClient.GetProductSellerVouchersAsync(651040, Arg.Any<CancellationToken>())
            .Returns(_ => LoadFixture("seller-vouchers.json"));
        var service = new GetSellerVouchersService(apiClient, NoCache);

        var result = await service.ExecuteAsync(651040);

        var voucher = Assert.Single(result);
        Assert.Equal("10 درصد تخفیف", voucher.Title);
        Assert.Equal(5000000, voucher.MinPurchaseRials);
        Assert.Equal(10, voucher.DiscountPercent);
        Assert.Equal(300000, voucher.MaxDiscountRials);
    }

    private static JsonDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiContracts", fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private sealed class NullCacheStore : ICacheStore
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);

        public Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken ct = default) where T : class => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;

        public Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default) => Task.CompletedTask;

        public Task CleanupExpiredAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
