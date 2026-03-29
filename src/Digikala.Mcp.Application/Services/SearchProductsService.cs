using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class SearchProductsService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<SearchResultDto> ExecuteAsync(
        string query,
        string? categorySlug = null,
        int page = 1,
        int? sort = null,
        double? minRating = null,
        int? minReviewCount = null,
        bool? onlyAvailable = null,
        long? minPriceRials = null,
        long? maxPriceRials = null,
        int? limit = null,
        IEnumerable<string>? preferTerms = null,
        IEnumerable<string>? avoidTerms = null,
        bool includeAttributeHighlights = true,
        bool includeAvailableFilters = true,
        bool preferBalancedMatches = false,
        bool prioritizeBestMatch = true,
        CancellationToken ct = default)
    {
        var normalizedCategorySlug = string.IsNullOrWhiteSpace(categorySlug) ? "search" : categorySlug.Trim();
        var normalizedPreferTerms = preferTerms?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        var normalizedAvoidTerms = avoidTerms?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        var request = new Dictionary<string, string?>
        {
            ["query"] = query,
            ["categorySlug"] = categorySlug,
            ["page"] = page.ToString(),
            ["sort"] = sort?.ToString(),
            ["minRating"] = minRating?.ToString(),
            ["minReviewCount"] = minReviewCount?.ToString(),
            ["onlyAvailable"] = onlyAvailable?.ToString(),
            ["minPriceRials"] = minPriceRials?.ToString(),
            ["maxPriceRials"] = maxPriceRials?.ToString(),
            ["limit"] = limit?.ToString(),
            ["preferTerms"] = normalizedPreferTerms is null ? null : string.Join(", ", normalizedPreferTerms),
            ["avoidTerms"] = normalizedAvoidTerms is null ? null : string.Join(", ", normalizedAvoidTerms),
            ["includeAttributeHighlights"] = includeAttributeHighlights.ToString(),
            ["includeAvailableFilters"] = includeAvailableFilters.ToString(),
            ["preferBalancedMatches"] = preferBalancedMatches.ToString(),
            ["prioritizeBestMatch"] = prioritizeBestMatch.ToString(),
        };

        var cacheKey = ToolExecutionHelper.CreateCacheKey(
            "search_products",
            ("query", query),
            ("category", normalizedCategorySlug),
            ("page", page),
            ("sort", sort),
            ("minRating", minRating),
            ("minReviewCount", minReviewCount),
            ("onlyAvailable", onlyAvailable),
            ("minPriceRials", minPriceRials),
            ("maxPriceRials", maxPriceRials),
            ("limit", limit),
            ("preferTerms", normalizedPreferTerms),
            ("avoidTerms", normalizedAvoidTerms),
            ("includeAttributeHighlights", includeAttributeHighlights),
            ("includeAvailableFilters", includeAvailableFilters),
            ("preferBalancedMatches", preferBalancedMatches),
            ("prioritizeBestMatch", prioritizeBestMatch));

        try
        {
            return await ToolExecutionHelper.GetOrCreateAsync(
                cache,
                cacheKey,
                CacheTier.ShortTerm,
                async token =>
                {
                    using var response = await apiClient.SearchCategoryAsync(
                        normalizedCategorySlug,
                        page,
                        sort,
                        query: query,
                        ct: token);

                    var result = mapper.MapToSearchResult(response);
                    return ApplyFilteringAndRanking(
                        result,
                        minRating,
                        minReviewCount,
                        onlyAvailable,
                        minPriceRials,
                        maxPriceRials,
                        limit,
                        normalizedPreferTerms,
                        normalizedAvoidTerms,
                        includeAttributeHighlights,
                        includeAvailableFilters,
                        preferBalancedMatches,
                        prioritizeBestMatch);
                },
                ct);
        }
        catch (Exception exception)
        {
            return new SearchResultDto
            {
                Warning = "Digikala search request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "search_products",
                    "Digikala search request failed.",
                    exception,
                    request),
                FilteredCount = 0,
                AppliedFilters = [],
            };
        }
    }

    private static SearchResultDto ApplyFilteringAndRanking(
        SearchResultDto result,
        double? minRating,
        int? minReviewCount,
        bool? onlyAvailable,
        long? minPriceRials,
        long? maxPriceRials,
        int? limit,
        IEnumerable<string>? preferTerms,
        IEnumerable<string>? avoidTerms,
        bool includeAttributeHighlights,
        bool includeAvailableFilters,
        bool preferBalancedMatches,
        bool prioritizeBestMatch)
    {
        var appliedFilters = new List<string>();
        IEnumerable<ProductSummaryDto> products = result.Products;

        if (minRating is not null)
        {
            appliedFilters.Add($"min_rating>={minRating:0.0}");
            products = products.Where(item => (item.Rating ?? 0) >= minRating.Value);
        }

        if (minReviewCount is not null)
        {
            appliedFilters.Add($"min_review_count>={minReviewCount}");
            products = products.Where(item => item.ReviewCount >= minReviewCount.Value);
        }

        if (onlyAvailable is not null)
        {
            appliedFilters.Add($"only_available={onlyAvailable.Value}");
            products = products.Where(item => item.IsAvailable == onlyAvailable.Value);
        }

        if (minPriceRials is not null)
        {
            appliedFilters.Add($"min_price_rials>={minPriceRials}");
            products = products.Where(item => item.PriceRials >= minPriceRials.Value);
        }

        if (maxPriceRials is not null)
        {
            appliedFilters.Add($"max_price_rials<={maxPriceRials}");
            products = products.Where(item => item.PriceRials <= maxPriceRials.Value);
        }

        var rankedProducts = products
            .Select(item =>
            {
                ProductSuitabilityAnalyzer.EnrichSummary(item, preferTerms, avoidTerms, includeAttributeHighlights, preferBalancedMatches);
                item.SearchScore = CalculateSearchScore(item, preferBalancedMatches);
                return item;
            });

        if (prioritizeBestMatch)
        {
            appliedFilters.Add("prioritize_best_match=true");
            rankedProducts = rankedProducts
                .OrderByDescending(item => item.SearchScore ?? double.MinValue)
                .ThenByDescending(item => item.Rating ?? 0)
                .ThenByDescending(item => item.ReviewCount)
                .ThenBy(item => item.PriceRials == 0 ? long.MaxValue : item.PriceRials);
        }

        var materialized = rankedProducts.ToList();
        if (limit is > 0)
        {
            appliedFilters.Add($"limit={limit}");
            materialized = materialized.Take(limit.Value).ToList();
        }

        if (preferTerms is not null)
        {
            appliedFilters.Add($"prefer_terms={string.Join(", ", preferTerms)}");
        }

        if (avoidTerms is not null)
        {
            appliedFilters.Add($"avoid_terms={string.Join(", ", avoidTerms)}");
        }

        if (preferBalancedMatches)
        {
            appliedFilters.Add("prefer_balanced_matches=true");
        }

        return result with
        {
            Products = materialized,
            BestMatch = materialized.FirstOrDefault(),
            FilteredCount = materialized.Count,
            AppliedFilters = appliedFilters,
            AvailableFilters = includeAvailableFilters ? result.AvailableFilters : [],
            AvailableSortOptions = includeAvailableFilters ? result.AvailableSortOptions : [],
        };
    }

    private static double CalculateSearchScore(ProductSummaryDto product, bool preferBalancedMatches)
    {
        var ratingComponent = (product.Rating ?? 0d) * 100d;
        var reviewConfidence = Math.Log10(Math.Max(1, product.ReviewCount)) * 20d;
        var availabilityBonus = product.IsAvailable ? 15d : 0d;
        var pricePenalty = product.PriceRials > 0 ? Math.Min(product.PriceRials / 10_000_000d, 15d) : 0d;
        var signalScore = ProductSuitabilityAnalyzer.CalculateSignalScore(product, preferBalancedMatches);

        return ratingComponent + reviewConfidence + availabilityBonus + signalScore - pricePenalty;
    }
}
