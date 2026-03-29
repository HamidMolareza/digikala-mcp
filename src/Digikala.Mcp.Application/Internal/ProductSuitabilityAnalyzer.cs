using System.Text;
using Digikala.Mcp.Application.Dtos;

namespace Digikala.Mcp.Application.Internal;

internal static class ProductSuitabilityAnalyzer
{
    private static readonly string[] IngredientTerms =
    [
        "آلوئه ورا",
        "پروتئین گندم",
        "کراتین",
        "آرگان",
        "روغن آرگان",
        "کافئین",
        "بیوتین",
        "زینک",
        "منتول",
        "رزماری",
        "بابونه",
        "سالیسیلیک"
    ];

    private static readonly string[] HighlightTerms =
    [
        "روزانه",
        "ملایم",
        "متعادل کننده",
        "بدون سولفات",
        "بدون پارابن",
        "موهای چرب",
        "کف سر چرب",
        "موهای خشک",
        "پوست سر حساس",
        "ضد شوره",
        "کنترل کننده چربی",
        "آبرسان",
        "تقویت کننده"
    ];

    private static readonly string[] CautionTerms =
    [
        "ضد شوره",
        "کنترل کننده چربی قوی",
        "کاهش دهنده چربی",
        "پاکسازی عمیق",
        "قوی"
    ];

    public static ProductSummaryDto EnrichSummary(
        ProductSummaryDto product,
        IEnumerable<string>? preferTerms = null,
        IEnumerable<string>? avoidTerms = null,
        bool includeAttributeHighlights = true,
        bool preferBalancedMatches = false)
    {
        var analysisText = BuildSummaryAnalysisText(product);
        ApplySuitabilitySignals(product, analysisText, includeAttributeHighlights);
        ApplyIntentSignals(product, analysisText, preferTerms, avoidTerms, preferBalancedMatches);
        return product;
    }

    public static ProductDetailDto EnrichDetail(ProductDetailDto detail)
    {
        var analysisText = BuildDetailAnalysisText(detail);
        ApplySuitabilitySignals(detail, analysisText, includeAttributeHighlights: true);
        ApplyIntentSignals(detail, analysisText, null, null, preferBalancedMatches: false);
        return detail;
    }

    public static double CalculateSignalScore(ProductSummaryDto product, bool preferBalancedMatches)
    {
        var score = 0d;
        score += product.MatchedSignals.Count * 35d;
        score -= product.RejectedSignals.Count * 45d;
        score += product.AttributeHighlights.Count * 6d;
        score += product.KeyIngredients.Count * 4d;
        score -= product.PotentialCautions.Count * 10d;

        if (product.IsDailyUse == true)
        {
            score += 18d;
        }

        if (product.IsSulfateFree == true)
        {
            score += 12d;
        }

        if (product.IsParabenFree == true)
        {
            score += 8d;
        }

        if (preferBalancedMatches)
        {
            if (product.UseCaseHints.Any(item => item.Contains("balanced", StringComparison.OrdinalIgnoreCase)))
            {
                score += 24d;
            }

            if (product.UseCaseHints.Any(item => item.Contains("daily", StringComparison.OrdinalIgnoreCase)))
            {
                score += 10d;
            }
        }

        return score;
    }

    private static void ApplySuitabilitySignals(ProductSummaryDto product, string analysisText, bool includeAttributeHighlights)
    {
        product.IsDailyUse ??= ContainsAny(analysisText, "روزانه", "daily");
        product.IsSulfateFree ??= ContainsAny(analysisText, "بدون سولفات", "sulfate free");
        product.IsParabenFree ??= ContainsAny(analysisText, "بدون پارابن", "paraben free");

        AddIfMatched(product.SuitableForHairTypes, "oily hair", analysisText, "موهای چرب");
        AddIfMatched(product.SuitableForHairTypes, "dry hair", analysisText, "موهای خشک");
        AddIfMatched(product.SuitableForHairTypes, "damaged hair", analysisText, "موهای آسیب", "آسیب دیده");

        AddIfMatched(product.SuitableForScalpTypes, "oily scalp", analysisText, "کف سر چرب", "پوست سر چرب");
        AddIfMatched(product.SuitableForScalpTypes, "sensitive scalp", analysisText, "پوست سر حساس", "کف سر حساس");

        AddIfMatched(product.ConcernsTargeted, "oil control", analysisText, "کنترل چربی", "متعادل کننده چربی", "کاهش دهنده چربی");
        AddIfMatched(product.ConcernsTargeted, "hydration", analysisText, "آبرسان", "رطوبت");
        AddIfMatched(product.ConcernsTargeted, "anti dandruff", analysisText, "ضد شوره");
        AddIfMatched(product.ConcernsTargeted, "strengthening", analysisText, "تقویت", "استحکام");

        foreach (var ingredient in IngredientTerms.Where(term => analysisText.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            AddDistinct(product.KeyIngredients, ingredient);
        }

        foreach (var caution in CautionTerms.Where(term => analysisText.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            AddDistinct(product.PotentialCautions, caution);
        }

        if (includeAttributeHighlights)
        {
            foreach (var highlight in HighlightTerms.Where(term => analysisText.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                AddDistinct(product.AttributeHighlights, highlight);
            }
        }
    }

    private static void ApplyIntentSignals(
        ProductSummaryDto product,
        string analysisText,
        IEnumerable<string>? preferTerms,
        IEnumerable<string>? avoidTerms,
        bool preferBalancedMatches)
    {
        if (preferTerms is not null)
        {
            foreach (var term in preferTerms.Where(term => !string.IsNullOrWhiteSpace(term)))
            {
                if (analysisText.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    AddDistinct(product.MatchedSignals, term.Trim());
                }
            }
        }

        if (avoidTerms is not null)
        {
            foreach (var term in avoidTerms.Where(term => !string.IsNullOrWhiteSpace(term)))
            {
                if (analysisText.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    AddDistinct(product.RejectedSignals, term.Trim());
                }
            }
        }

        if (product.IsDailyUse == true)
        {
            AddDistinct(product.UseCaseHints, "daily use");
        }

        if (ContainsAny(analysisText, "ملایم", "متعادل کننده", "متعادل"))
        {
            AddDistinct(product.UseCaseHints, "balanced cleansing");
        }

        if (preferBalancedMatches && product.RejectedSignals.Count == 0 && product.MatchedSignals.Count > 0)
        {
            AddDistinct(product.SuitabilityNotes, "matches the preferred need without obvious harsh-treatment signals");
        }

        if (product.RejectedSignals.Count > 0)
        {
            AddDistinct(product.SuitabilityNotes, "contains terms that may indicate a stronger or less suitable match");
        }

        if (product.PotentialCautions.Count > 0)
        {
            AddDistinct(product.SuitabilityNotes, $"potential cautions: {string.Join(", ", product.PotentialCautions)}");
        }
    }

    private static string BuildSummaryAnalysisText(ProductSummaryDto product)
    {
        var builder = new StringBuilder();
        builder.Append(product.Title).Append(' ');
        builder.Append(product.EnglishTitle).Append(' ');
        builder.Append(product.CategorySlug);
        return builder.ToString();
    }

    private static string BuildDetailAnalysisText(ProductDetailDto detail)
    {
        var builder = new StringBuilder(BuildSummaryAnalysisText(detail));
        builder.Append(' ').Append(detail.Description);
        builder.Append(' ').Append(detail.Brand);
        builder.Append(' ').Append(detail.Warranty);

        foreach (var spec in detail.Specifications)
        {
            builder.Append(' ').Append(spec.GroupTitle).Append(' ').Append(spec.Title).Append(' ').Append(spec.Value);
        }

        if (detail.ProsCons is not null)
        {
            foreach (var item in detail.ProsCons.Pros)
            {
                builder.Append(' ').Append(item);
            }

            foreach (var item in detail.ProsCons.Cons)
            {
                builder.Append(' ').Append(item);
            }
        }

        return builder.ToString();
    }

    private static void AddIfMatched(ICollection<string> target, string value, string text, params string[] terms)
    {
        if (ContainsAny(text, terms))
        {
            AddDistinct(target, value);
        }
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static void AddDistinct(ICollection<string> target, string value)
    {
        if (!target.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            target.Add(value);
        }
    }
}
