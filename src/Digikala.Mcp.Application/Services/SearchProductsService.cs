using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Domain.ValueObjects;

namespace Digikala.Mcp.Application.Services;

public sealed class SearchProductsService(
    IDigikalaApiClient apiClient,
    ICacheStore cache,
    ProductMapper mapper,
    CategoryResolutionService categoryResolutionService)
{
    private const int MaxUpstreamPages = 3;
    private const int DefaultResultTarget = 20;

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
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var normalizedCategorySlug = NormalizeCategorySlug(categorySlug);
        var normalizedPreferTerms = preferTerms?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        var normalizedAvoidTerms = avoidTerms?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        var unvalidatedRequest = CreateRequest(
            normalizedQuery,
            normalizedCategorySlug,
            resolvedCategorySlug: null,
            resolutionSource: "unvalidated",
            page,
            sort,
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
            prioritizeBestMatch,
            skipCache);

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return CreateValidationError(
                "Search query is required.",
                unvalidatedRequest,
                "Provide a non-empty query string before calling search_products.");
        }

        var validationDiagnostics = ValidateInputs(page, minRating, minReviewCount, minPriceRials, maxPriceRials, limit);
        if (validationDiagnostics.Count > 0)
        {
            return CreateValidationError(
                "search_products received invalid filter or pagination inputs.",
                unvalidatedRequest,
                validationDiagnostics.ToArray());
        }

        var resolution = await categoryResolutionService.ResolveAsync(normalizedQuery, normalizedCategorySlug, skipCache, ct);
        var resolvedCategorySlug = resolution.CategorySlug;
        var request = CreateRequest(
            normalizedQuery,
            normalizedCategorySlug,
            resolvedCategorySlug,
            resolution.ResolutionSource,
            page,
            sort,
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
            prioritizeBestMatch,
            skipCache);

        var resolutionWarning = resolvedCategorySlug is null
            ? "No category was given or inferred; used Digikala's global keyword search. Pass categorySlug to narrow results and enable exact price filtering."
            : resolution.WasInferred ? resolution.Diagnostics.FirstOrDefault() : null;

        var cacheKey = ToolExecutionHelper.CreateCacheKey(
            "search_products",
            ("v", 2),
            ("query", normalizedQuery),
            ("category", resolvedCategorySlug),
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
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
                cache,
                cacheKey,
                CacheTier.ShortTerm,
                async token =>
                {
                    var result = await FetchCandidatesAsync(
                        normalizedQuery,
                        resolvedCategorySlug,
                        page,
                        sort,
                        minRating,
                        minReviewCount,
                        onlyAvailable,
                        minPriceRials,
                        maxPriceRials,
                        limit,
                        normalizedAvoidTerms,
                        token);

                    result = result with
                    {
                        Warning = CombineWarnings(result.Warning, resolutionWarning),
                    };

                    return ApplyFilteringAndRanking(
                        result,
                        normalizedQuery,
                        sort,
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
                skipCache,
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

    private async Task<SearchResultDto> FetchCandidatesAsync(
        string query,
        string? categorySlug,
        int page,
        int? sort,
        double? minRating,
        int? minReviewCount,
        bool? onlyAvailable,
        long? minPriceRials,
        long? maxPriceRials,
        int? limit,
        IReadOnlyCollection<string>? avoidTerms,
        CancellationToken ct)
    {
        // Rating, review-count and avoid-term filters only run locally, so a single
        // upstream page can shrink to a handful of products; read a few more pages.
        var needsMorePages = minRating is not null || minReviewCount is not null || avoidTerms is { Count: > 0 };
        var target = limit ?? DefaultResultTarget;
        var products = new List<ProductSummaryDto>();
        var seenIds = new HashSet<long>();
        SearchResultDto? firstPage = null;
        var pagesFetched = 0;
        var currentPage = page;

        while (true)
        {
            using var response = await apiClient.SearchProductsAsync(
                new ProductSearchRequest(
                    query,
                    categorySlug,
                    currentPage,
                    sort,
                    minPriceRials,
                    maxPriceRials,
                    OnlyAvailable: onlyAvailable == true),
                ct);

            var pageResult = mapper.MapToSearchResult(response);
            firstPage ??= pageResult;
            pagesFetched++;
            products.AddRange(pageResult.Products.Where(item => item.Id == 0 || seenIds.Add(item.Id)));

            var hasMorePages = currentPage < pageResult.TotalPages && pageResult.Products.Count > 0;
            if (!needsMorePages
                || !hasMorePages
                || pagesFetched >= MaxUpstreamPages
                || CountPassingFilters(products, minRating, minReviewCount, onlyAvailable, minPriceRials, maxPriceRials) >= target)
            {
                break;
            }

            currentPage++;
        }

        return firstPage with
        {
            Products = products,
            SearchedCategorySlug = categorySlug,
            PagesFetched = pagesFetched,
        };
    }

    private static int CountPassingFilters(
        IEnumerable<ProductSummaryDto> products,
        double? minRating,
        int? minReviewCount,
        bool? onlyAvailable,
        long? minPriceRials,
        long? maxPriceRials) =>
        products.Count(item =>
            (minRating is null || (NormalizeRatingToFiveScale(item.Rating) ?? 0) >= minRating.Value)
            && (minReviewCount is null || item.ReviewCount >= minReviewCount.Value)
            && (onlyAvailable is null || item.IsAvailable == onlyAvailable.Value)
            && (minPriceRials is null || item.PriceRials >= minPriceRials.Value)
            && (maxPriceRials is null || item.PriceRials <= maxPriceRials.Value));

    private static SearchResultDto ApplyFilteringAndRanking(
        SearchResultDto result,
        string query,
        int? sort,
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
        // Products are records with value equality, so positions are tracked by reference.
        var upstreamPositions = new Dictionary<ProductSummaryDto, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < result.Products.Count; index++)
        {
            upstreamPositions[result.Products[index]] = index;
        }

        var upstreamCount = result.Products.Count;
        IEnumerable<ProductSummaryDto> products = result.Products;

        if (minRating is not null)
        {
            appliedFilters.Add($"min_rating>={minRating:0.0}");
            products = products.Where(item => (NormalizeRatingToFiveScale(item.Rating) ?? 0) >= minRating.Value);
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

        var queryTokens = SearchTextMatcher.Tokenize(query);
        var rankedProducts = products
            .Select(item =>
            {
                ProductSuitabilityAnalyzer.EnrichSummary(item, preferTerms, avoidTerms, includeAttributeHighlights, preferBalancedMatches);
                item.SearchScore = CalculateSearchScore(
                    item,
                    upstreamPositions[item],
                    upstreamCount,
                    queryTokens,
                    preferBalancedMatches);
                return item;
            });

        // An explicit non-relevance sort (cheapest, newest, ...) is the caller's ranking; keep Digikala's order.
        if (prioritizeBestMatch && IsRelevanceSort(sort))
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

    // Keyword match outweighs everything else, then Digikala's own relevance order;
    // rating and review volume only separate otherwise similar matches.
    private static double CalculateSearchScore(
        ProductSummaryDto product,
        int upstreamIndex,
        int upstreamCount,
        IReadOnlyList<string> queryTokens,
        bool preferBalancedMatches)
    {
        var keywordComponent = SearchTextMatcher.MatchRatio(queryTokens, product.Title, product.EnglishTitle) * 120d;
        var positionComponent = upstreamCount <= 1 ? 40d : 40d * (1d - (double)upstreamIndex / upstreamCount);
        var ratingComponent = (NormalizeRatingToFiveScale(product.Rating) ?? 0d) * 6d;
        var reviewConfidence = Math.Log10(Math.Max(1, product.ReviewCount)) * 5d;
        var availabilityBonus = product.IsAvailable ? 10d : 0d;
        var signalScore = ProductSuitabilityAnalyzer.CalculateSignalScore(product, preferBalancedMatches);

        return Math.Round(
            keywordComponent + positionComponent + ratingComponent + reviewConfidence + availabilityBonus + signalScore,
            2);
    }

    private static bool IsRelevanceSort(int? sort) => sort is null || sort == (int)SortOption.MostRelevant;

    // Digikala now reports ratings as a 0-100 percentage; older payloads used 0-5.
    internal static double? NormalizeRatingToFiveScale(double? rating) =>
        rating is > 5 ? rating / 20d : rating;

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        return query.Trim().TrimEnd('.', '،', ',', ';', '؛');
    }

    private static string? NormalizeCategorySlug(string? categorySlug)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            return null;
        }

        var normalized = categorySlug.Trim();
        return string.Equals(normalized, "search", StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }

    private static string? CombineWarnings(params string?[] warnings)
    {
        var materialized = warnings
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return materialized.Count == 0
            ? null
            : string.Join(" ", materialized);
    }

    private static Dictionary<string, string?> CreateRequest(
        string normalizedQuery,
        string? normalizedCategorySlug,
        string? resolvedCategorySlug,
        string resolutionSource,
        int page,
        int? sort,
        double? minRating,
        int? minReviewCount,
        bool? onlyAvailable,
        long? minPriceRials,
        long? maxPriceRials,
        int? limit,
        IEnumerable<string>? normalizedPreferTerms,
        IEnumerable<string>? normalizedAvoidTerms,
        bool includeAttributeHighlights,
        bool includeAvailableFilters,
        bool preferBalancedMatches,
        bool prioritizeBestMatch,
        bool skipCache)
    {
        return new Dictionary<string, string?>
        {
            ["query"] = normalizedQuery,
            ["categorySlug"] = normalizedCategorySlug,
            ["resolvedCategorySlug"] = resolvedCategorySlug,
            ["resolutionSource"] = resolutionSource,
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
            ["skipCache"] = skipCache.ToString(),
        };
    }

    private static SearchResultDto CreateValidationError(string message, Dictionary<string, string?> request, params string[] diagnostics)
    {
        return new SearchResultDto
        {
            Warning = "Structured validation blocked an unsupported Digikala search request.",
            Error = new ToolIssueDto
            {
                Code = "InvalidInput",
                Message = message,
                Tool = "search_products",
                Request = request,
                Retryable = false,
                Diagnostics = diagnostics
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            },
            FilteredCount = 0,
            AppliedFilters = [],
        };
    }

    private static List<string> ValidateInputs(
        int page,
        double? minRating,
        int? minReviewCount,
        long? minPriceRials,
        long? maxPriceRials,
        int? limit)
    {
        var diagnostics = new List<string>();

        if (page < 1)
        {
            diagnostics.Add("`page` must be greater than or equal to 1.");
        }

        if (minRating is < 0 or > 5)
        {
            diagnostics.Add("`minRating` must be between 0 and 5.");
        }

        if (minReviewCount is < 0)
        {
            diagnostics.Add("`minReviewCount` must be greater than or equal to 0.");
        }

        if (minPriceRials is < 0)
        {
            diagnostics.Add("`minPriceRials` must be greater than or equal to 0.");
        }

        if (maxPriceRials is < 0)
        {
            diagnostics.Add("`maxPriceRials` must be greater than or equal to 0.");
        }

        if (minPriceRials is not null && maxPriceRials is not null && minPriceRials > maxPriceRials)
        {
            diagnostics.Add("`minPriceRials` cannot be greater than `maxPriceRials`.");
        }

        if (limit is <= 0)
        {
            diagnostics.Add("`limit` must be greater than or equal to 1 when provided.");
        }

        return diagnostics;
    }
}
