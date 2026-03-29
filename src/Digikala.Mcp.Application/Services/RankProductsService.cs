using Digikala.Mcp.Application.Dtos;

namespace Digikala.Mcp.Application.Services;

public sealed class RankProductsService(SearchProductsService searchProductsService)
{
    private static readonly HashSet<string> SupportedRankingPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "buyer_popularity",
        "rating",
        "rating_count",
        "best_match",
        "price_low",
        "price_high",
        "availability",
    };

    public async Task<RankProductsResultDto> ExecuteAsync(
        string query,
        string? categorySlug = null,
        bool? onlyAvailable = true,
        IEnumerable<string>? requiredTerms = null,
        IEnumerable<string>? preferredTerms = null,
        IEnumerable<string>? excludedTerms = null,
        IEnumerable<string>? rankingPriority = null,
        int limit = 5,
        double? minRating = null,
        int? minReviewCount = null,
        long? minPriceRials = null,
        long? maxPriceRials = null,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var normalizedRequiredTerms = NormalizeTerms(requiredTerms);
        var normalizedPreferredTerms = NormalizeTerms(preferredTerms);
        var normalizedExcludedTerms = NormalizeTerms(excludedTerms);
        var normalizedRankingPriority = NormalizeTerms(rankingPriority);

        if (normalizedRankingPriority.Count == 0)
        {
            normalizedRankingPriority = ["buyer_popularity", "rating", "rating_count"];
        }

        var invalidPriority = normalizedRankingPriority.FirstOrDefault(item => !SupportedRankingPriorities.Contains(item));
        if (invalidPriority is not null)
        {
            return CreateValidationError(
                query,
                categorySlug,
                normalizedRankingPriority,
                $"Unsupported ranking priority `{invalidPriority}`.",
                "Supported values: buyer_popularity, rating, rating_count, best_match, price_low, price_high, availability.");
        }

        var searchResult = await searchProductsService.ExecuteAsync(
            query: query,
            categorySlug: categorySlug,
            page: 1,
            sort: 1,
            minRating: minRating,
            minReviewCount: minReviewCount,
            onlyAvailable: onlyAvailable,
            minPriceRials: minPriceRials,
            maxPriceRials: maxPriceRials,
            limit: Math.Clamp(limit * 4, 8, 40),
            preferTerms: normalizedRequiredTerms.Concat(normalizedPreferredTerms).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            avoidTerms: normalizedExcludedTerms,
            includeAttributeHighlights: true,
            includeAvailableFilters: false,
            preferBalancedMatches: false,
            prioritizeBestMatch: false,
            skipCache: skipCache,
            ct: ct);

        if (searchResult.Error is not null && searchResult.Products.Count == 0)
        {
            return new RankProductsResultDto
            {
                IsStale = searchResult.IsStale,
                IsFromCache = searchResult.IsFromCache,
                CacheStatus = searchResult.CacheStatus,
                Warning = searchResult.Warning,
                Error = searchResult.Error,
                SearchQuery = query,
                CategorySlug = categorySlug,
                AppliedRankingPriority = normalizedRankingPriority,
                AppliedFilters = searchResult.AppliedFilters,
            };
        }

        var filteredCandidates = searchResult.Products
            .Where(product => MatchesRequiredTerms(product, normalizedRequiredTerms))
            .Where(product => !MatchesExcludedTerms(product, normalizedExcludedTerms))
            .ToList();

        var orderedCandidates = ApplyRanking(filteredCandidates, normalizedRankingPriority)
            .Take(Math.Max(1, limit))
            .ToList();

        return new RankProductsResultDto
        {
            IsStale = searchResult.IsStale,
            IsFromCache = searchResult.IsFromCache,
            CacheStatus = searchResult.CacheStatus,
            Warning = searchResult.Warning,
            SearchQuery = query,
            CategorySlug = categorySlug,
            CandidateCount = filteredCandidates.Count,
            AppliedRankingPriority = normalizedRankingPriority,
            AppliedFilters = searchResult.AppliedFilters
                .Concat(normalizedRequiredTerms.Count == 0 ? [] : [$"required_terms={string.Join(", ", normalizedRequiredTerms)}"])
                .Concat(normalizedPreferredTerms.Count == 0 ? [] : [$"preferred_terms={string.Join(", ", normalizedPreferredTerms)}"])
                .Concat(normalizedExcludedTerms.Count == 0 ? [] : [$"excluded_terms={string.Join(", ", normalizedExcludedTerms)}"])
                .ToList(),
            Products = orderedCandidates
                .Select((product, index) => new RankedProductDto
                {
                    Rank = index + 1,
                    Id = product.Id,
                    Title = product.Title,
                    IsAvailable = product.IsAvailable,
                    PriceRials = product.PriceRials,
                    Url = product.Url,
                    AbsoluteUrl = product.AbsoluteUrl,
                    Rating = product.Rating,
                    RatingCount = product.RatingCount,
                    ReviewCount = product.ReviewCount,
                    SearchScore = product.SearchScore,
                    WhyRankedHere = BuildWhyRankedHere(product, normalizedRequiredTerms, normalizedPreferredTerms, normalizedRankingPriority),
                })
                .ToList(),
        };
    }

    private static RankProductsResultDto CreateValidationError(
        string query,
        string? categorySlug,
        List<string> rankingPriority,
        string message,
        string diagnostic)
    {
        return new RankProductsResultDto
        {
            Warning = message,
            Error = new ToolIssueDto
            {
                Code = "InvalidInput",
                Message = message,
                Tool = "rank_products",
                Request = new Dictionary<string, string?>
                {
                    ["query"] = query,
                    ["categorySlug"] = categorySlug,
                    ["rankingPriority"] = string.Join(", ", rankingPriority),
                },
                Retryable = false,
                Diagnostics = [diagnostic],
            },
            SearchQuery = query,
            CategorySlug = categorySlug,
            AppliedRankingPriority = rankingPriority,
        };
    }

    private static List<string> NormalizeTerms(IEnumerable<string>? values) =>
        values?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? [];

    private static IEnumerable<ProductSummaryDto> ApplyRanking(IEnumerable<ProductSummaryDto> candidates, IReadOnlyList<string> priorities)
    {
        IOrderedEnumerable<ProductSummaryDto>? ordered = null;

        foreach (var priority in priorities)
        {
            ordered = ordered is null
                ? ApplyPrimaryOrdering(candidates, priority)
                : ApplySecondaryOrdering(ordered, priority);
        }

        return (ordered ?? candidates.OrderByDescending(item => item.SearchScore ?? 0))
            .ThenByDescending(item => item.SearchScore ?? 0)
            .ThenByDescending(item => item.Rating ?? 0)
            .ThenByDescending(item => item.RatingCount)
            .ThenBy(item => item.PriceRials == 0 ? long.MaxValue : item.PriceRials);
    }

    private static IOrderedEnumerable<ProductSummaryDto> ApplyPrimaryOrdering(IEnumerable<ProductSummaryDto> candidates, string priority) => priority switch
    {
        "buyer_popularity" => candidates.OrderByDescending(item => item.RatingCount),
        "rating" => candidates.OrderByDescending(item => item.Rating ?? 0),
        "rating_count" => candidates.OrderByDescending(item => item.RatingCount),
        "best_match" => candidates.OrderByDescending(item => item.SearchScore ?? 0),
        "price_low" => candidates.OrderBy(item => item.PriceRials == 0 ? long.MaxValue : item.PriceRials),
        "price_high" => candidates.OrderByDescending(item => item.PriceRials),
        "availability" => candidates.OrderByDescending(item => item.IsAvailable),
        _ => candidates.OrderByDescending(item => item.SearchScore ?? 0),
    };

    private static IOrderedEnumerable<ProductSummaryDto> ApplySecondaryOrdering(IOrderedEnumerable<ProductSummaryDto> candidates, string priority) => priority switch
    {
        "buyer_popularity" => candidates.ThenByDescending(item => item.RatingCount),
        "rating" => candidates.ThenByDescending(item => item.Rating ?? 0),
        "rating_count" => candidates.ThenByDescending(item => item.RatingCount),
        "best_match" => candidates.ThenByDescending(item => item.SearchScore ?? 0),
        "price_low" => candidates.ThenBy(item => item.PriceRials == 0 ? long.MaxValue : item.PriceRials),
        "price_high" => candidates.ThenByDescending(item => item.PriceRials),
        "availability" => candidates.ThenByDescending(item => item.IsAvailable),
        _ => candidates.ThenByDescending(item => item.SearchScore ?? 0),
    };

    private static bool MatchesRequiredTerms(ProductSummaryDto product, IReadOnlyCollection<string> requiredTerms) =>
        requiredTerms.Count == 0 || requiredTerms.All(term => GetSearchableText(product).Contains(term, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesExcludedTerms(ProductSummaryDto product, IReadOnlyCollection<string> excludedTerms) =>
        excludedTerms.Count > 0 && excludedTerms.Any(term => GetSearchableText(product).Contains(term, StringComparison.OrdinalIgnoreCase));

    private static string GetSearchableText(ProductSummaryDto product)
    {
        return string.Join(
            " ",
            new[]
            {
                product.Title,
                product.EnglishTitle,
                string.Join(' ', product.AttributeHighlights),
                string.Join(' ', product.MatchedSignals),
                string.Join(' ', product.UseCaseHints),
            }.Where(item => !string.IsNullOrWhiteSpace(item)));
    }

    private static List<string> BuildWhyRankedHere(
        ProductSummaryDto product,
        IReadOnlyCollection<string> requiredTerms,
        IReadOnlyCollection<string> preferredTerms,
        IReadOnlyList<string> rankingPriority)
    {
        var reasons = new List<string>();

        if (requiredTerms.Count > 0 && MatchesRequiredTerms(product, requiredTerms))
        {
            reasons.Add("matched required terms");
        }

        if (preferredTerms.Count > 0)
        {
            var matchedPreferredTerms = preferredTerms
                .Where(term => GetSearchableText(product).Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();

            if (matchedPreferredTerms.Count > 0)
            {
                reasons.Add($"matched preferred terms: {string.Join(", ", matchedPreferredTerms)}");
            }
        }

        foreach (var priority in rankingPriority)
        {
            switch (priority)
            {
                case "buyer_popularity" when product.RatingCount > 0:
                    reasons.Add($"strong buyer popularity via {product.RatingCount} ratings");
                    break;
                case "rating" when product.Rating is not null:
                    reasons.Add($"high rating: {product.Rating:0.0}/5");
                    break;
                case "rating_count" when product.RatingCount > 0:
                    reasons.Add($"strong rating count: {product.RatingCount}");
                    break;
                case "best_match" when product.SearchScore is not null:
                    reasons.Add($"strong search match score: {product.SearchScore:0.0}");
                    break;
                case "price_low" when product.PriceRials > 0:
                    reasons.Add("lower price prioritized");
                    break;
                case "price_high" when product.PriceRials > 0:
                    reasons.Add("premium price prioritized");
                    break;
                case "availability" when product.IsAvailable:
                    reasons.Add("currently available");
                    break;
            }
        }

        return reasons
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }
}
