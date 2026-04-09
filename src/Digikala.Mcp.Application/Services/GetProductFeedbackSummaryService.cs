using System.Text.RegularExpressions;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductFeedbackSummaryService(
    GetProductDetailService detailService,
    GetProductReviewsService reviewsService)
{
    private static readonly ThemeDefinition[] ThemeDefinitions =
    [
        new("drying_risk",
            ["خشک", "خشکی", "زبر", "stipping", "stripping", "rough"],
            ["خشک نمی", "خشک نمیکنه", "ملایم", "آبرسان", "non-drying", "not drying"]),
        new("mildness",
            ["ملایم", "متعادل", "روزانه", "بدون سولفات", "gentle", "balanced", "قوی نیست", "not strong"],
            ["کنترل کننده چربی قوی", "خشن", "strong oil control", "harsh"]),
        new("daily_use",
            ["روزانه", "هر روز", "daily"],
            ["درمان", "treatment"]),
        new("oil_control_strength",
            ["کنترل چربی", "کنترل کننده چربی قوی", "کاهش دهنده چربی", "ضد شوره", "strong oil control"],
            ["ملایم", "متعادل", "قوی نیست", "not strong"])
    ];

    public async Task<ProductFeedbackSummaryDto> ExecuteAsync(
        long productId,
        int maxPros = 8,
        int maxCons = 8,
        int maxThemes = 8,
        bool includeReviewSnippets = true,
        int maxSnippets = 6,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["maxPros"] = maxPros.ToString(),
            ["maxCons"] = maxCons.ToString(),
            ["maxThemes"] = maxThemes.ToString(),
            ["includeReviewSnippets"] = includeReviewSnippets.ToString(),
            ["maxSnippets"] = maxSnippets.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };

        var validationDiagnostics = ValidateInputs(productId, maxPros, maxCons, maxThemes, maxSnippets);
        if (validationDiagnostics.Count > 0)
        {
            return new ProductFeedbackSummaryDto
            {
                ProductId = productId,
                Warning = "Structured validation blocked an invalid feedback summary request.",
                Error = new ToolIssueDto
                {
                    Code = "InvalidInput",
                    Message = "get_product_feedback_summary received invalid inputs.",
                    Tool = "get_product_feedback_summary",
                    Request = request,
                    Retryable = false,
                    Diagnostics = validationDiagnostics,
                },
            };
        }

        try
        {
            var detailTask = detailService.ExecuteAsync(productId, null, skipCache, ct);
            var reviewsTask = reviewsService.ExecuteAsync(productId, 1, skipCache, ct);
            await Task.WhenAll(detailTask, reviewsTask);

            var detail = await detailTask;
            var reviews = await reviewsTask;
            var evidenceTexts = BuildEvidenceTexts(detail, reviews);
            var detailPros = (detail.ProsCons?.Pros ?? []).Concat(detail.BuyerCommentsSummary?.Pros ?? []);
            var detailCons = (detail.ProsCons?.Cons ?? []).Concat(detail.BuyerCommentsSummary?.Cons ?? []);

            var pros = AggregateCounts(detailPros, reviews.Reviews.SelectMany(item => item.Pros), maxPros);
            var cons = AggregateCounts(detailCons, reviews.Reviews.SelectMany(item => item.Cons), maxCons);
            var themes = BuildThemes(evidenceTexts, includeReviewSnippets, maxThemes, maxSnippets);

            return new ProductFeedbackSummaryDto
            {
                ProductId = productId,
                IsStale = detail.IsStale || reviews.IsStale,
                IsFromCache = detail.IsFromCache || reviews.IsFromCache,
                CacheStatus = BuildCompositeCacheStatus(detail.CacheStatus, reviews.CacheStatus),
                Warning = detail.Warning ?? reviews.Warning,
                Pros = pros,
                Cons = cons,
                Themes = themes,
                Summary = BuildSummary(themes, pros, cons),
            };
        }
        catch (Exception exception)
        {
            return new ProductFeedbackSummaryDto
            {
                ProductId = productId,
                Warning = "Could not build product feedback summary; structured diagnostics returned.",
                Error = ToolIssueFactory.Create(
                    "get_product_feedback_summary",
                    "Product feedback summary could not be generated.",
                    exception,
                    request),
            };
        }
    }

    private static string BuildCompositeCacheStatus(params string[] statuses)
    {
        var distinct = statuses
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

    private static List<FeedbackCountDto> AggregateCounts(
        IEnumerable<string>? primary,
        IEnumerable<string>? secondary,
        int limit)
    {
        return (primary ?? []).Concat(secondary ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .GroupBy(item => item, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FeedbackCountDto { Label = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    private static List<EvidenceText> BuildEvidenceTexts(ProductDetailDto detail, ReviewListDto reviews)
    {
        var evidence = new List<EvidenceText>();

        if (!string.IsNullOrWhiteSpace(detail.Description))
        {
            evidence.Add(new EvidenceText(detail.Description, "detail"));
        }

        if (detail.ProsCons is not null)
        {
            evidence.AddRange(detail.ProsCons.Pros.Select(item => new EvidenceText(item, "detail-pro")));
            evidence.AddRange(detail.ProsCons.Cons.Select(item => new EvidenceText(item, "detail-con")));
        }

        if (detail.BuyerCommentsSummary is not null)
        {
            if (!string.IsNullOrWhiteSpace(detail.BuyerCommentsSummary.Summary))
            {
                evidence.Add(new EvidenceText(detail.BuyerCommentsSummary.Summary, "buyer-summary"));
            }

            evidence.AddRange(detail.BuyerCommentsSummary.Pros.Select(item => new EvidenceText(item, "buyer-summary-pro")));
            evidence.AddRange(detail.BuyerCommentsSummary.Cons.Select(item => new EvidenceText(item, "buyer-summary-con")));
        }

        evidence.AddRange(reviews.Reviews.Select(review => new EvidenceText(
            string.Join(" ", new[] { review.Title, review.Body }.Where(item => !string.IsNullOrWhiteSpace(item))),
            "review")));

        return evidence.Where(item => !string.IsNullOrWhiteSpace(item.Text)).ToList();
    }

    private static List<FeedbackThemeDto> BuildThemes(
        List<EvidenceText> evidenceTexts,
        bool includeReviewSnippets,
        int maxThemes,
        int maxSnippets)
    {
        return ThemeDefinitions
            .Select(definition =>
            {
                var positiveMatches = evidenceTexts
                    .Where(item => ContainsThemeSignal(item.Text, definition.PositiveTerms, definition.NegativeTerms))
                    .ToList();
                var negativeMatches = evidenceTexts
                    .Where(item => ContainsThemeSignal(item.Text, definition.NegativeTerms, definition.PositiveTerms))
                    .ToList();
                var evidenceCount = positiveMatches.Count + negativeMatches.Count;
                if (evidenceCount == 0)
                {
                    return null;
                }

                return new FeedbackThemeDto
                {
                    Name = definition.Name,
                    Signal = CalculateSignal(definition.Name, positiveMatches.Count, negativeMatches.Count),
                    EvidenceCount = evidenceCount,
                    SampleSnippets = includeReviewSnippets
                        ? positiveMatches
                            .Concat(negativeMatches)
                            .Select(item => SanitizeSnippet(item.Text))
                            .Where(item => !string.IsNullOrWhiteSpace(item))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Take(Math.Max(1, maxSnippets))
                            .ToList()
                        : [],
                };
            })
            .Where(item => item is not null)
            .Cast<FeedbackThemeDto>()
            .OrderByDescending(item => item.EvidenceCount)
            .Take(Math.Max(1, maxThemes))
            .ToList();
    }

    private static string BuildSummary(
        List<FeedbackThemeDto> themes,
        List<FeedbackCountDto> pros,
        List<FeedbackCountDto> cons)
    {
        var mildness = themes.FirstOrDefault(item => item.Name == "mildness")?.Signal;
        var dryingRisk = themes.FirstOrDefault(item => item.Name == "drying_risk")?.Signal;
        var oilStrength = themes.FirstOrDefault(item => item.Name == "oil_control_strength")?.Signal;

        if (mildness is "high" or "medium-high" && dryingRisk is "low" or "medium-low")
        {
            return "Mostly described as balanced and relatively gentle, with limited evidence of over-drying.";
        }

        if (oilStrength is "high" && dryingRisk is ("medium-high" or "high"))
        {
            return "Evidence suggests a stronger oil-control profile, with a meaningful chance of feeling drying for some users.";
        }

        if (pros.Count > cons.Count)
        {
            return "Feedback leans positive overall, but assistants should still inspect the theme signals for specific cautions.";
        }

        return "Feedback is mixed; inspect the theme signals and snippets before making a confident recommendation.";
    }

    private static string CalculateSignal(string themeName, int positiveMatches, int negativeMatches)
    {
        var delta = positiveMatches - negativeMatches;

        return delta switch
        {
            >= 4 => "high",
            2 or 3 => "medium-high",
            1 => "medium-low",
            0 => "medium",
            -1 => "medium-low",
            <= -2 => "low",
        };
    }

    private static bool ContainsThemeSignal(
        string text,
        IEnumerable<string> wantedTerms,
        IEnumerable<string> opposingTerms)
    {
        var hasWanted = wantedTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
        if (!hasWanted)
        {
            return false;
        }

        return !opposingTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeSnippet(string value)
    {
        var compact = Regex.Replace(value, @"\s+", " ").Trim();
        return compact.Length <= 180 ? compact : $"{compact[..177]}...";
    }

    private static List<string> ValidateInputs(long productId, int maxPros, int maxCons, int maxThemes, int maxSnippets)
    {
        var diagnostics = new List<string>();

        if (productId <= 0)
        {
            diagnostics.Add("`productId` must be greater than 0.");
        }

        if (maxPros < 1)
        {
            diagnostics.Add("`maxPros` must be greater than or equal to 1.");
        }

        if (maxCons < 1)
        {
            diagnostics.Add("`maxCons` must be greater than or equal to 1.");
        }

        if (maxThemes < 1)
        {
            diagnostics.Add("`maxThemes` must be greater than or equal to 1.");
        }

        if (maxSnippets < 1)
        {
            diagnostics.Add("`maxSnippets` must be greater than or equal to 1.");
        }

        return diagnostics;
    }

    private sealed record ThemeDefinition(string Name, string[] PositiveTerms, string[] NegativeTerms);
    private sealed record EvidenceText(string Text, string Source);
}
