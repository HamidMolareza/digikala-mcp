using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Server.Tools;

[McpServerToolType]
public sealed class SearchProductsTool(SearchProductsService service, ILogger<SearchProductsTool> logger)
{
    [McpServerTool, Description("Search products by keyword within an optional category, with filters, sorting, and pagination.")]
    public Task<SearchResultDto> search_products(
        [Description("Search keyword in Persian or English")] string query,
        [Description("Category slug, e.g. 'mobile-phone'. Null for all categories.")] string? categorySlug = null,
        [Description("Page number, starting from 1")] int page = 1,
        [Description("Sort: 1=relevance, 4=popular, 7=cheapest, 8=expensive, 25=best-selling, 27=newest")] int? sort = null,
        [Description("Optional minimum rating threshold, e.g. 4.5")] double? minRating = null,
        [Description("Optional minimum review count threshold, e.g. 500")] int? minReviewCount = null,
        [Description("Only return currently available products when true.")] bool? onlyAvailable = null,
        [Description("Optional minimum selling price in rials.")] long? minPriceRials = null,
        [Description("Optional maximum selling price in rials.")] long? maxPriceRials = null,
        [Description("Maximum number of products to keep after filtering and ranking.")] int? limit = null,
        [Description("Terms to positively boost when ranking candidates.")] string[]? preferTerms = null,
        [Description("Terms to penalize when ranking candidates.")] string[]? avoidTerms = null,
        [Description("Include extracted attribute highlights in product summaries.")] bool includeAttributeHighlights = true,
        [Description("Include available filters and sort options in the response.")] bool includeAvailableFilters = true,
        [Description("Prefer balanced and daily-use matches over harsher treatment-style matches.")] bool preferBalancedMatches = false,
        [Description("Rank matches to surface the best overall choice first.")] bool prioritizeBestMatch = true,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Tool search_products invoked: query={Query}, category={Category}, page={Page}, sort={Sort}, minRating={MinRating}, minReviewCount={MinReviewCount}, onlyAvailable={OnlyAvailable}, minPriceRials={MinPriceRials}, maxPriceRials={MaxPriceRials}, limit={Limit}, preferTerms={PreferTerms}, avoidTerms={AvoidTerms}, includeAttributeHighlights={IncludeAttributeHighlights}, includeAvailableFilters={IncludeAvailableFilters}, preferBalancedMatches={PreferBalancedMatches}, prioritizeBestMatch={PrioritizeBestMatch}",
            query,
            categorySlug,
            page,
            sort,
            minRating,
            minReviewCount,
            onlyAvailable,
            minPriceRials,
            maxPriceRials,
            limit,
            preferTerms,
            avoidTerms,
            includeAttributeHighlights,
            includeAvailableFilters,
            preferBalancedMatches,
            prioritizeBestMatch);

        return service.ExecuteAsync(
            query,
            categorySlug,
            page,
            sort,
            minRating,
            minReviewCount,
            onlyAvailable,
            minPriceRials,
            maxPriceRials,
            limit,
            preferTerms,
            avoidTerms,
            includeAttributeHighlights,
            includeAvailableFilters,
            preferBalancedMatches,
            prioritizeBestMatch,
            ct);
    }
}

[McpServerToolType]
public sealed class GetProductFeedbackSummaryTool(GetProductFeedbackSummaryService service, ILogger<GetProductFeedbackSummaryTool> logger)
{
    [McpServerTool, Description("Return compact evidence-based feedback signals for one product using detail and review data.")]
    public Task<ProductFeedbackSummaryDto> get_product_feedback_summary(
        [Description("Numeric product ID")] long productId,
        [Description("Maximum number of pro labels to return.")] int maxPros = 8,
        [Description("Maximum number of con labels to return.")] int maxCons = 8,
        [Description("Maximum number of themes to return.")] int maxThemes = 8,
        [Description("Include short review snippets as evidence.")] bool includeReviewSnippets = true,
        [Description("Maximum number of snippets across themes.")] int maxSnippets = 6,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Tool get_product_feedback_summary invoked: productId={ProductId}, maxPros={MaxPros}, maxCons={MaxCons}, maxThemes={MaxThemes}, includeReviewSnippets={IncludeReviewSnippets}, maxSnippets={MaxSnippets}",
            productId,
            maxPros,
            maxCons,
            maxThemes,
            includeReviewSnippets,
            maxSnippets);

        return service.ExecuteAsync(productId, maxPros, maxCons, maxThemes, includeReviewSnippets, maxSnippets, ct);
    }
}

[McpServerToolType]
public sealed class GetProductDetailTool(GetProductDetailService service, ILogger<GetProductDetailTool> logger)
{
    [McpServerTool, Description("Get full product detail including variants, images, price, seller, and specs.")]
    public Task<ProductDetailDto> get_product_detail(
        [Description("Numeric product ID")] long productId,
        [Description("Warehouse ID for Fresh products. Null for main store.")] int? warehouseId = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_product_detail invoked: productId={ProductId}, warehouseId={WarehouseId}", productId, warehouseId);
        return service.ExecuteAsync(productId, warehouseId, ct);
    }
}

[McpServerToolType]
public sealed class GetProductOverviewTool(GetProductOverviewService service, ILogger<GetProductOverviewTool> logger)
{
    [McpServerTool, Description("Get the most useful combined product data in one call: detail, price chart, top reviews, top questions, and recommendations.")]
    public Task<ProductOverviewDto> get_product_overview(
        [Description("Numeric product ID")] long productId,
        [Description("Warehouse ID for Fresh products. Null for main store.")] int? warehouseId = null,
        [Description("Number of top reviews to return.")] int topReviewsCount = 3,
        [Description("Number of top questions to return.")] int topQuestionsCount = 3,
        [Description("Number of recommendations to return.")] int topRecommendationsCount = 6,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Tool get_product_overview invoked: productId={ProductId}, warehouseId={WarehouseId}, topReviewsCount={TopReviewsCount}, topQuestionsCount={TopQuestionsCount}, topRecommendationsCount={TopRecommendationsCount}",
            productId,
            warehouseId,
            topReviewsCount,
            topQuestionsCount,
            topRecommendationsCount);

        return service.ExecuteAsync(productId, warehouseId, topReviewsCount, topQuestionsCount, topRecommendationsCount, ct);
    }
}

[McpServerToolType]
public sealed class GetProductPriceChartTool(GetProductPriceChartService service, ILogger<GetProductPriceChartTool> logger)
{
    [McpServerTool, Description("Get historical price data for a product.")]
    public Task<PriceChartDto> get_product_price_chart(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_product_price_chart invoked: productId={ProductId}", productId);
        return service.ExecuteAsync(productId, ct);
    }
}

[McpServerToolType]
public sealed class GetProductReviewsTool(GetProductReviewsService service, ILogger<GetProductReviewsTool> logger)
{
    [McpServerTool, Description("Get paginated user reviews for a product.")]
    public Task<ReviewListDto> get_product_reviews(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_product_reviews invoked: productId={ProductId}, page={Page}", productId, page);
        return service.ExecuteAsync(productId, page, ct);
    }
}

[McpServerToolType]
public sealed class GetProductQuestionsTool(GetProductQuestionsService service, ILogger<GetProductQuestionsTool> logger)
{
    [McpServerTool, Description("Get paginated Q&A for a product.")]
    public Task<QuestionListDto> get_product_questions(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_product_questions invoked: productId={ProductId}, page={Page}", productId, page);
        return service.ExecuteAsync(productId, page, ct);
    }
}

[McpServerToolType]
public sealed class GetProductRecommendationsTool(GetProductRecommendationsService service, ILogger<GetProductRecommendationsTool> logger)
{
    [McpServerTool, Description("Get recommended/similar products for a given product.")]
    public Task<List<ProductSummaryDto>> get_product_recommendations(
        [Description("Numeric product ID")] long productId,
        [Description("Offset for pagination, starting from 0")] int offset = 0,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_product_recommendations invoked: productId={ProductId}, offset={Offset}", productId, offset);
        return service.ExecuteAsync(productId, offset, ct);
    }
}

[McpServerToolType]
public sealed class GetBestSellingTool(GetBestSellingService service, ILogger<GetBestSellingTool> logger)
{
    [McpServerTool, Description("Get best-selling products, optionally by category and time window.")]
    public Task<List<ProductSummaryDto>> get_best_selling(
        [Description("Numeric category ID. Null for all.")] int? categoryId = null,
        [Description("Time window filter, e.g. 'month'")] string? lastDays = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_best_selling invoked: categoryId={CategoryId}, lastDays={LastDays}", categoryId, lastDays);
        return service.ExecuteAsync(categoryId, lastDays, ct);
    }
}

[McpServerToolType]
public sealed class GetIncredibleOffersTool(GetIncredibleOffersService service, ILogger<GetIncredibleOffersTool> logger)
{
    [McpServerTool, Description("Get products currently on incredible/lightning deals.")]
    public Task<OffersListDto> get_incredible_offers(
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_incredible_offers invoked: page={Page}", page);
        return service.ExecuteAsync(page, ct);
    }
}

[McpServerToolType]
public sealed class GetTrendingProductsTool(GetTrendingProductsService service, ILogger<GetTrendingProductsTool> logger)
{
    [McpServerTool, Description("Get trending products.")]
    public Task<List<ProductSummaryDto>> get_trending_products(CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_trending_products invoked");
        return service.ExecuteAsync(ct);
    }
}

[McpServerToolType]
public sealed class GetCategoryLandingTool(GetCategoryLandingService service, ILogger<GetCategoryLandingTool> logger)
{
    [McpServerTool, Description("Get a category landing page with subcategories, popular brands, and highlights.")]
    public Task<CategoryDto> get_category_landing(
        [Description("Category slug, e.g. 'electronic-devices'")] string categorySlug,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_category_landing invoked: categorySlug={CategorySlug}", categorySlug);
        return service.ExecuteAsync(categorySlug, ct);
    }
}

[McpServerToolType]
public sealed class BrowseByTagTool(BrowseByTagService service, ILogger<BrowseByTagTool> logger)
{
    [McpServerTool, Description("Browse products by SEO tag (e.g. 'football-boots', 'iphone-16').")]
    public Task<SearchResultDto> browse_by_tag(
        [Description("Tag slug, e.g. 'iphone-16'")] string tagSlug,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool browse_by_tag invoked: tagSlug={TagSlug}, page={Page}", tagSlug, page);
        return service.ExecuteAsync(tagSlug, page, ct);
    }
}

[McpServerToolType]
public sealed class AutocompleteTool(AutocompleteService service, ILogger<AutocompleteTool> logger)
{
    [McpServerTool, Description("Get search autocomplete suggestions and trends for a query prefix.")]
    public Task<AutocompleteDto> autocomplete(
        [Description("Search query prefix")] string query,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool autocomplete invoked: query={Query}", query);
        return service.ExecuteAsync(query, ct);
    }
}

[McpServerToolType]
public sealed class GetBrandPageTool(GetBrandPageService service, ILogger<GetBrandPageTool> logger)
{
    [McpServerTool, Description("Get a premium brand landing page.")]
    public Task<BrandPageDto> get_brand_page(
        [Description("Brand slug, e.g. 'persil'")] string brandSlug,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_brand_page invoked: brandSlug={BrandSlug}", brandSlug);
        return service.ExecuteAsync(brandSlug, ct);
    }
}

[McpServerToolType]
public sealed class GetSellerVouchersTool(GetSellerVouchersService service, ILogger<GetSellerVouchersTool> logger)
{
    [McpServerTool, Description("Get seller vouchers/promotions for a product.")]
    public Task<List<SellerVoucherDto>> get_seller_vouchers(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        logger.LogInformation("Tool get_seller_vouchers invoked: productId={ProductId}", productId);
        return service.ExecuteAsync(productId, ct);
    }
}
