using System.Text.RegularExpressions;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Services;

public sealed class RecommendProductsForNeedService(
    SearchProductsService searchProductsService,
    GetProductFeedbackSummaryService feedbackSummaryService)
{
    private static readonly string[] CableNeedTerms =
    [
        "کابل",
        "کابل شارژ",
        "charging cable",
        "data cable",
        "usb-c",
        "usb c",
        "type-c",
        "type c",
        "تایپ سی",
    ];

    private static readonly string[] UsbCConnectorTerms =
    [
        "usb-c",
        "usb c",
        "type-c",
        "type c",
        "تایپ سی",
    ];

    private static readonly string[] XiaomiPhoneTerms =
    [
        "xiaomi",
        "شیائومی",
        "redmi",
        "ردمی",
        "poco",
        "پوکو",
        "note 10",
        "نوت 10",
    ];

    private static readonly string[] FastChargeTerms =
    [
        "fast charge",
        "fast charging",
        "quick charge",
        "شارژ سریع",
        "فست شارژ",
        "3a",
        "66w",
        "67w",
        "120w",
    ];

    private static readonly string[] DurableCableTerms =
    [
        "durable",
        "sturdy",
        "braided",
        "دوام",
        "دوام خوب",
        "مقاوم",
        "بادوام",
        "کنفی",
    ];

    private static readonly string[] IncompatibleCableTerms =
    [
        "lightning",
        "لایتنینگ",
        "micro-usb",
        "micro usb",
        "microusb",
        "میکرو usb",
    ];

    private static readonly string[] PersianStrongOilControlTerms =
    [
        "کنترل کننده چربی قوی",
        "کاهش دهنده چربی",
        "ضد شوره",
    ];

    private static readonly string[] EnglishStrongOilControlTerms =
    [
        "strong oil control",
        "anti-dandruff",
        "deep cleansing",
    ];

    public async Task<ProductNeedRecommendationsDto> ExecuteAsync(
        string needDescription,
        string? categorySlug = null,
        int limit = 5,
        bool? onlyAvailable = true,
        double? minRating = null,
        int? minReviewCount = null,
        long? minPriceRials = null,
        long? maxPriceRials = null,
        IEnumerable<string>? preferTerms = null,
        IEnumerable<string>? avoidTerms = null,
        bool includeEvidence = true,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var understanding = BuildUnderstanding(needDescription, preferTerms, avoidTerms);
        var request = new Dictionary<string, string?>
        {
            ["needDescription"] = needDescription,
            ["categorySlug"] = categorySlug,
            ["limit"] = limit.ToString(),
            ["onlyAvailable"] = onlyAvailable?.ToString(),
            ["minRating"] = minRating?.ToString(),
            ["minReviewCount"] = minReviewCount?.ToString(),
            ["minPriceRials"] = minPriceRials?.ToString(),
            ["maxPriceRials"] = maxPriceRials?.ToString(),
            ["preferTerms"] = understanding.PreferTerms.Count == 0 ? null : string.Join(", ", understanding.PreferTerms),
            ["avoidTerms"] = understanding.AvoidTerms.Count == 0 ? null : string.Join(", ", understanding.AvoidTerms),
            ["includeEvidence"] = includeEvidence.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };

        var validationDiagnostics = ValidateInputs(needDescription, limit, minRating, minReviewCount, minPriceRials, maxPriceRials);
        if (validationDiagnostics.Count > 0)
        {
            return new ProductNeedRecommendationsDto
            {
                Warning = "Structured validation blocked an invalid recommendation request.",
                Error = new ToolIssueDto
                {
                    Code = "InvalidInput",
                    Message = "recommend_products_for_need received invalid filter or pagination inputs.",
                    Tool = "recommend_products_for_need",
                    Request = request,
                    Retryable = false,
                    Diagnostics = validationDiagnostics,
                },
                SearchQuery = understanding.SearchQuery,
                QueryUnderstanding = understanding.ToDto(),
            };
        }

        try
        {
            var candidateLimit = GetCandidateLimit(limit, includeEvidence);
            var searchResult = await searchProductsService.ExecuteAsync(
                query: understanding.SearchQuery,
                categorySlug: categorySlug,
                page: 1,
                sort: 1,
                minRating: minRating,
                minReviewCount: minReviewCount,
                onlyAvailable: onlyAvailable,
                minPriceRials: minPriceRials,
                maxPriceRials: maxPriceRials,
                limit: candidateLimit,
                preferTerms: understanding.PreferTerms,
                avoidTerms: understanding.AvoidTerms,
                includeAttributeHighlights: true,
                includeAvailableFilters: false,
                preferBalancedMatches: understanding.PreferBalancedMatches,
                prioritizeBestMatch: true,
                skipCache: skipCache,
                ct: ct);

            if (searchResult.Error is not null && searchResult.Products.Count == 0)
            {
                return new ProductNeedRecommendationsDto
                {
                    IsStale = searchResult.IsStale,
                    IsFromCache = searchResult.IsFromCache,
                    CacheStatus = searchResult.CacheStatus,
                    Warning = searchResult.Warning,
                    Error = searchResult.Error,
                    SearchQuery = understanding.SearchQuery,
                    QueryUnderstanding = understanding.ToDto(),
                };
            }

            var candidates = searchResult.Products.Take(candidateLimit).ToList();
            var candidateEvaluations = await EvaluateCandidatesAsync(candidates, includeEvidence, skipCache, ct);
            var scoredCandidates = candidateEvaluations
                .Select(item => BuildRecommendation(item, understanding, includeEvidence))
                .OrderByDescending(item => item.RawScore)
                .ToList();

            var visibleCandidates = scoredCandidates.Take(Math.Max(1, limit)).ToList();
            var minScore = visibleCandidates.MinByOrDefault(item => item.RawScore)?.RawScore ?? 0d;
            var maxScore = visibleCandidates.MaxByOrDefault(item => item.RawScore)?.RawScore ?? minScore;
            var candidateSelectionWarning = searchResult.TotalCount > candidateLimit
                ? $"Feedback evidence was evaluated for the top {candidateLimit} ranked search matches to keep upstream fan-out bounded."
                : null;

            return new ProductNeedRecommendationsDto
            {
                IsStale = searchResult.IsStale || visibleCandidates.Any(item => item.Feedback.IsStale),
                IsFromCache = searchResult.IsFromCache || visibleCandidates.Any(item => item.Feedback.IsFromCache),
                CacheStatus = BuildRecommendationCacheStatus(searchResult.CacheStatus, visibleCandidates.Select(item => item.Feedback.CacheStatus)),
                Warning = CombineWarnings(searchResult.Warning, candidateSelectionWarning),
                SearchQuery = understanding.SearchQuery,
                QueryUnderstanding = understanding.ToDto(),
                Recommendations = visibleCandidates
                    .Select(item => item.ToDto(NormalizeScore(item.RawScore, minScore, maxScore)))
                    .ToList(),
            };
        }
        catch (Exception exception)
        {
            return new ProductNeedRecommendationsDto
            {
                Warning = "Could not complete the recommendation workflow; structured diagnostics returned.",
                Error = ToolIssueFactory.Create(
                    "recommend_products_for_need",
                    "Recommendation workflow could not be completed.",
                    exception,
                    request),
                SearchQuery = understanding.SearchQuery,
                QueryUnderstanding = understanding.ToDto(),
            };
        }
    }

    private async Task<List<CandidateEvaluation>> EvaluateCandidatesAsync(
        IEnumerable<ProductSummaryDto> candidates,
        bool includeEvidence,
        bool skipCache,
        CancellationToken ct)
    {
        const int maxConcurrentCandidates = 2;
        using var throttler = new SemaphoreSlim(maxConcurrentCandidates);

        var tasks = candidates.Select(async product =>
        {
            await throttler.WaitAsync(ct);
            try
            {
                return new CandidateEvaluation(
                    product,
                    await feedbackSummaryService.ExecuteAsync(
                        product.Id,
                        maxPros: 4,
                        maxCons: 4,
                        maxThemes: 4,
                        includeReviewSnippets: includeEvidence,
                        maxSnippets: includeEvidence ? 3 : 1,
                        skipCache: skipCache,
                        ct: ct));
            }
            finally
            {
                throttler.Release();
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private static int GetCandidateLimit(int limit, bool includeEvidence)
    {
        var buffer = includeEvidence ? 2 : 1;
        return Math.Clamp(Math.Max(limit + buffer, 3), 3, 6);
    }

    private static string BuildRecommendationCacheStatus(string primaryStatus, IEnumerable<string> candidateStatuses)
    {
        var distinct = new[] { primaryStatus }
            .Concat(candidateStatuses)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return distinct.Length switch
        {
            0 => "unknown",
            1 => distinct[0],
            _ => string.Join('+', distinct.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)),
        };
    }

    private static string? CombineWarnings(params string?[] warnings)
    {
        var parts = warnings
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(" ", parts);
    }

    private static RecommendationUnderstanding BuildUnderstanding(
        string needDescription,
        IEnumerable<string>? preferTerms,
        IEnumerable<string>? avoidTerms)
    {
        var requiredSignals = new List<string>();
        var preferredSignals = new List<string>();
        var avoidSignals = new List<string>();
        var normalizedNeed = needDescription.Trim();
        var isPersian = Regex.IsMatch(normalizedNeed, "[\\u0600-\\u06FF]");
        var isCableNeed = ContainsAny(normalizedNeed, CableNeedTerms);

        if (ContainsAny(normalizedNeed, "شامپو", "shampoo"))
        {
            requiredSignals.Add(isPersian ? "shampoo" : "shampoo");
        }

        if (ContainsAny(normalizedNeed, "موهای چرب", "موی چرب", "کف سر چرب", "oily hair", "oily scalp"))
        {
            requiredSignals.Add("oily hair");
        }

        if (ContainsAny(normalizedNeed, "خشک", "dry lengths", "dry hair", "موهای خشک"))
        {
            preferredSignals.Add("supports dry lengths");
        }

        if (ContainsAny(
            normalizedNeed,
            "ملایم",
            "متعادل",
            "روزانه",
            "gentle",
            "mild",
            "balanced",
            "not too strong",
            "قوی نباشد",
            "خیلی قوی نباشد"))
        {
            preferredSignals.AddRange(["mild", "balanced cleansing"]);
        }

        if (ContainsAny(
            normalizedNeed,
            "خشک نکند",
            "خشک نمی کند",
            "خشک نشود",
            "non-drying",
            "not drying",
            "over-dry"))
        {
            preferredSignals.Add("non-drying");
            avoidSignals.Add("drying risk");
        }

        if (ContainsAny(normalizedNeed, "روزانه", "daily"))
        {
            preferredSignals.Add("daily use");
        }

        if (isCableNeed)
        {
            requiredSignals.Add(isPersian ? "کابل شارژ" : "charging cable");

            if (ContainsAny(normalizedNeed, UsbCConnectorTerms))
            {
                preferredSignals.Add(isPersian ? "USB-C" : "usb-c");
            }

            if (ContainsAny(normalizedNeed, XiaomiPhoneTerms))
            {
                preferredSignals.Add(isPersian ? "USB به USB-C" : "usb to usb-c");
            }

            if (ContainsAny(normalizedNeed, FastChargeTerms))
            {
                preferredSignals.AddRange(isPersian ? ["شارژ سریع", "3A"] : ["fast charge", "3a"]);
            }

            if (ContainsAny(normalizedNeed, DurableCableTerms))
            {
                preferredSignals.AddRange(isPersian ? ["بادوام", "مقاوم"] : ["durable", "braided"]);
            }

            avoidSignals.AddRange(IncompatibleCableTerms);
        }

        var mergedPreferred = MergeTerms(preferredSignals, preferTerms);
        var mergedAvoid = MergeTerms(
            avoidSignals,
            avoidTerms,
            mergedPreferred.Contains("mild", StringComparer.OrdinalIgnoreCase) || mergedPreferred.Contains("balanced cleansing", StringComparer.OrdinalIgnoreCase)
                ? (isPersian ? PersianStrongOilControlTerms : EnglishStrongOilControlTerms)
                : []);

        var searchQuery = BuildSearchQuery(normalizedNeed, requiredSignals, isPersian);

        return new RecommendationUnderstanding(
            searchQuery,
            MergeTerms(requiredSignals),
            mergedPreferred,
            mergedAvoid,
            PreferBalancedMatches: mergedPreferred.Contains("mild", StringComparer.OrdinalIgnoreCase)
                || mergedPreferred.Contains("balanced cleansing", StringComparer.OrdinalIgnoreCase)
                || mergedPreferred.Contains("non-drying", StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildSearchQuery(string needDescription, List<string> requiredSignals, bool isPersian)
    {
        if (ContainsAny(needDescription, CableNeedTerms))
        {
            return ContainsAny(needDescription, XiaomiPhoneTerms)
                ? (isPersian ? "کابل شارژ USB به USB-C" : "usb to usb-c charging cable")
                : (isPersian ? "کابل شارژ تایپ سی" : "usb-c charging cable");
        }

        if (requiredSignals.Contains("oily hair", StringComparer.OrdinalIgnoreCase)
            && ContainsAny(needDescription, "شامپو", "shampoo"))
        {
            return isPersian ? "شامپو موهای چرب" : "shampoo oily hair";
        }

        var keywords = Regex.Matches(needDescription, @"[\p{L}\p{N}]+")
            .Select(match => match.Value.Trim())
            .Where(token => token.Length > 1)
            .Where(token => !StopWords.Contains(token, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        return keywords.Length > 0
            ? string.Join(' ', keywords)
            : needDescription;
    }

    private static RecommendationCandidate BuildRecommendation(
        CandidateEvaluation candidate,
        RecommendationUnderstanding understanding,
        bool includeEvidence)
    {
        var product = candidate.Product;
        var feedback = candidate.Feedback;
        var why = new List<string>();
        var warnings = new List<string>();
        var reviewSignals = new List<string>();
        var snippets = new List<string>();
        var rawScore = product.SearchScore ?? 0d;

        if (product.UseCaseHints.Count > 0)
        {
            why.AddRange(product.UseCaseHints.Take(2));
        }

        if (product.MatchedSignals.Count > 0)
        {
            why.AddRange(product.MatchedSignals.Take(3).Select(item => $"Matched signal: {item}"));
        }

        if (product.PotentialCautions.Count > 0)
        {
            warnings.AddRange(product.PotentialCautions.Take(2));
        }

        if (product.RejectedSignals.Count > 0)
        {
            warnings.AddRange(product.RejectedSignals.Take(2).Select(item => $"Potential mismatch: {item}"));
        }

        rawScore += ApplyThemeAdjustments(feedback, understanding, why, warnings, reviewSignals, snippets);

        if (feedback.Error is not null)
        {
            warnings.Add("Review evidence is partial because feedback aggregation was not fully available.");
            rawScore -= 8d;
        }

        if (feedback.Cons.Count > 0)
        {
            warnings.AddRange(feedback.Cons.Take(1).Select(item => $"Frequent con: {item.Label}"));
        }

        why = why.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
        warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
        reviewSignals = reviewSignals.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
        snippets = snippets.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();

        return new RecommendationCandidate(
            product,
            feedback,
            rawScore,
            why,
            warnings,
            includeEvidence
                ? new ProductNeedRecommendationEvidenceDto
                {
                    MatchedSignals = product.MatchedSignals
                        .Concat(product.AttributeHighlights)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(6)
                        .ToList(),
                    ReviewSignals = reviewSignals,
                    FeedbackSummary = feedback.Summary,
                    FeedbackSnippets = snippets,
                }
                : null);
    }

    private static double ApplyThemeAdjustments(
        ProductFeedbackSummaryDto feedback,
        RecommendationUnderstanding understanding,
        List<string> why,
        List<string> warnings,
        List<string> reviewSignals,
        List<string> snippets)
    {
        var adjustment = 0d;

        foreach (var theme in feedback.Themes)
        {
            reviewSignals.Add($"{theme.Name}:{theme.Signal}");
            snippets.AddRange(theme.SampleSnippets);

            switch (theme.Name)
            {
                case "mildness":
                    if (understanding.PreferBalancedMatches)
                    {
                        adjustment += ScoreSignal(theme.Signal, positiveWeight: 18d, mediumWeight: 10d, negativeWeight: -14d);
                    }

                    if (theme.Signal is "high" or "medium-high")
                    {
                        why.Add("Review feedback points to a milder product profile.");
                    }
                    else if (theme.Signal == "low")
                    {
                        warnings.Add("Review feedback suggests a less gentle profile.");
                    }

                    break;

                case "drying_risk":
                    adjustment += ScoreSignal(theme.Signal, positiveWeight: -24d, mediumWeight: -12d, negativeWeight: 14d);
                    if (theme.Signal == "low")
                    {
                        why.Add("Review evidence suggests limited over-drying risk.");
                    }
                    else if (theme.Signal is "medium-high" or "high")
                    {
                        warnings.Add("Review evidence suggests a meaningful drying risk.");
                    }

                    break;

                case "daily_use":
                    if (understanding.PreferredSignals.Contains("daily use", StringComparer.OrdinalIgnoreCase))
                    {
                        adjustment += ScoreSignal(theme.Signal, positiveWeight: 10d, mediumWeight: 5d, negativeWeight: -8d);
                    }

                    if (theme.Signal is "high" or "medium-high")
                    {
                        why.Add("Review feedback supports daily-use suitability.");
                    }

                    break;

                case "oil_control_strength":
                    if (understanding.PreferBalancedMatches)
                    {
                        adjustment += ScoreSignal(theme.Signal, positiveWeight: -18d, mediumWeight: -9d, negativeWeight: 8d);
                    }

                    if (theme.Signal is "high" or "medium-high")
                    {
                        warnings.Add("Review feedback points to a stronger oil-control profile.");
                    }
                    else if (theme.Signal == "low")
                    {
                        why.Add("Review feedback suggests a more balanced cleansing profile.");
                    }

                    break;
            }
        }

        return adjustment;
    }

    private static double ScoreSignal(string signal, double positiveWeight, double mediumWeight, double negativeWeight) =>
        signal switch
        {
            "high" => positiveWeight,
            "medium-high" => mediumWeight,
            "medium-low" => mediumWeight / 2d,
            "low" => negativeWeight,
            _ => 0d,
        };

    private static double NormalizeScore(double value, double minScore, double maxScore)
    {
        if (Math.Abs(maxScore - minScore) < 0.001d)
        {
            return 1d;
        }

        return Math.Round(0.5d + ((value - minScore) / (maxScore - minScore)) * 0.5d, 2, MidpointRounding.AwayFromZero);
    }

    private static List<string> MergeTerms(params IEnumerable<string>?[] groups) =>
        groups
            .Where(group => group is not null)
            .SelectMany(group => group!)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static List<string> ValidateInputs(
        string needDescription,
        int limit,
        double? minRating,
        int? minReviewCount,
        long? minPriceRials,
        long? maxPriceRials)
    {
        var diagnostics = new List<string>();

        if (string.IsNullOrWhiteSpace(needDescription))
        {
            diagnostics.Add("`needDescription` must be a non-empty string.");
        }

        if (limit < 1)
        {
            diagnostics.Add("`limit` must be greater than or equal to 1.");
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

        return diagnostics;
    }

    private static readonly string[] StopWords =
    [
        "a",
        "an",
        "and",
        "for",
        "i",
        "is",
        "it",
        "me",
        "my",
        "of",
        "that",
        "the",
        "this",
        "to",
        "want",
        "with",
        "and",
        "but",
        "not",
        "too",
        "something",
        "من",
        "برای",
        "که",
        "می",
        "میخواهم",
        "نمی",
        "نه",
        "خیلی",
        "زیاد",
        "یک",
    ];

    private sealed record RecommendationUnderstanding(
        string SearchQuery,
        List<string> RequiredSignals,
        List<string> PreferredSignals,
        List<string> AvoidSignals,
        bool PreferBalancedMatches)
    {
        public NeedQueryUnderstandingDto ToDto() => new()
        {
            RequiredSignals = RequiredSignals,
            PreferredSignals = PreferredSignals,
            AvoidSignals = AvoidSignals,
        };

        public List<string> PreferTerms => PreferredSignals;
        public List<string> AvoidTerms => AvoidSignals;
    }

    private sealed record CandidateEvaluation(ProductSummaryDto Product, ProductFeedbackSummaryDto Feedback);

    private sealed record RecommendationCandidate(
        ProductSummaryDto Product,
        ProductFeedbackSummaryDto Feedback,
        double RawScore,
        List<string> Why,
        List<string> Warnings,
        ProductNeedRecommendationEvidenceDto? Evidence)
    {
        public ProductNeedRecommendationDto ToDto(double normalizedScore) => new()
        {
            Product = Product,
            Score = normalizedScore,
            Why = Why,
            Warnings = Warnings,
            Evidence = Evidence,
        };
    }
}

file static class RecommendationCandidateEnumerableExtensions
{
    public static T? MaxByOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return default;
        }

        var best = enumerator.Current;
        var bestValue = selector(best);
        while (enumerator.MoveNext())
        {
            var currentValue = selector(enumerator.Current);
            if (currentValue > bestValue)
            {
                best = enumerator.Current;
                bestValue = currentValue;
            }
        }

        return best;
    }

    public static T? MinByOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return default;
        }

        var best = enumerator.Current;
        var bestValue = selector(best);
        while (enumerator.MoveNext())
        {
            var currentValue = selector(enumerator.Current);
            if (currentValue < bestValue)
            {
                best = enumerator.Current;
                bestValue = currentValue;
            }
        }

        return best;
    }
}
