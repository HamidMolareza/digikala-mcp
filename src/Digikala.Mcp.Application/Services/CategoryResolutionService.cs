using System.Text.RegularExpressions;

namespace Digikala.Mcp.Application.Services;

public sealed class CategoryResolutionService(AutocompleteService autocompleteService)
{
    private static readonly CategoryAliasRule[] AliasRules =
    [
        new(
            "hair-shampoo",
            [
                "شامپو",
                "shampoo",
            ]),
        new(
            "power-bank",
            [
                "پاوربانک",
                "پاور بانک",
                "شارژر همراه",
                "powerbank",
                "power bank",
                "battery pack",
            ]),
        new(
            "cell-phone-data-cable",
            [
                "کابل",
                "کابل شارژ",
                "شارژر کابل",
                "usb-c",
                "usb c",
                "type-c",
                "type c",
                "تایپ سی",
                "charging cable",
                "data cable",
            ]),
        new(
            "mobile-phone",
            [
                "گوشی",
                "موبایل",
                "mobile",
                "phone",
                "iphone",
                "آیفون",
                "samsung",
                "سامسونگ",
                "xiaomi",
                "شیائومی",
                "pixel",
                "پیکسل",
                "redmi",
                "ردمی",
                "poco",
                "پوکو",
            ]),
        new(
            "electronic-component",
            [
                "کلید گردان",
                "چنج اور",
                "چنجور",
                "change over",
                "changeover",
                "rotary switch",
                "انتقال برق",
            ]),
        new(
            "power-supply",
            [
                "تبدیل برق شهر",
                "برق شهر ژنراتور",
                "مبدل برق",
                "ترانس تبدیل",
            ]),
        new(
            "power-strip",
            [
                "محافظ جان",
                "محافظ ولتاژ",
                "محافظ برق",
                "residual current",
            ]),
    ];

    public async Task<CategoryResolutionResult> ResolveAsync(
        string query,
        string? categorySlug = null,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var normalizedCategorySlug = NormalizeCategorySlug(categorySlug);
        if (normalizedCategorySlug is not null)
        {
            return CategoryResolutionResult.Explicit(normalizedCategorySlug);
        }

        var normalizedQuery = NormalizeQuery(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return CategoryResolutionResult.Unresolved("Search query is empty after normalization.");
        }

        var aliasMatch = TryMatchAlias(normalizedQuery);
        if (aliasMatch is not null)
        {
            var matchedTerms = aliasMatch.Value.MatchedTerms
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return CategoryResolutionResult.Inferred(
                aliasMatch.Value.Rule.CategorySlug,
                "alias-rule",
                $"Category inferred as `{aliasMatch.Value.Rule.CategorySlug}` from known query terms: {string.Join(", ", matchedTerms)}.");
        }

        try
        {
            var autocomplete = await autocompleteService.ExecuteAsync(normalizedQuery, skipCache, ct);
            var rankedCategories = autocomplete.Suggestions
                .Where(item => !string.IsNullOrWhiteSpace(item.CategorySlug))
                .GroupBy(item => item.CategorySlug!, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    CategorySlug = group.Key,
                    Score = group.Count() * 10
                        + group.Count(item => string.Equals(item.Text, normalizedQuery, StringComparison.OrdinalIgnoreCase)) * 5
                        + group.Count(item => normalizedQuery.Contains(item.Text, StringComparison.OrdinalIgnoreCase)),
                    SampleTerms = group
                        .Select(item => item.Text)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .ToArray(),
                })
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.SampleTerms.Length)
                .ThenBy(item => item.CategorySlug, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bestCategory = rankedCategories.FirstOrDefault();
            if (bestCategory is not null)
            {
                return CategoryResolutionResult.Inferred(
                    bestCategory.CategorySlug,
                    "autocomplete",
                    $"Category inferred as `{bestCategory.CategorySlug}` from autocomplete hints: {string.Join(", ", bestCategory.SampleTerms)}.");
            }

            return CategoryResolutionResult.Unresolved(
                "No category slug was provided, no known category aliases matched the query, and autocomplete returned no category hints.");
        }
        catch (Exception exception)
        {
            return CategoryResolutionResult.Unresolved(
                "No category slug was provided and category resolution could not use autocomplete hints.",
                $"Autocomplete lookup failed while resolving the category: {exception.Message}");
        }
    }

    private static (CategoryAliasRule Rule, string[] MatchedTerms)? TryMatchAlias(string query)
    {
        foreach (var rule in AliasRules)
        {
            var matchedTerms = rule.Terms
                .Where(term => query.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchedTerms.Length > 0)
            {
                return (rule, matchedTerms);
            }
        }

        return null;
    }

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var normalized = query.Trim().TrimEnd('.', '،', ',', ';', '؛');
        normalized = Regex.Replace(normalized, "\\s+", " ");
        return normalized;
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

    private sealed record CategoryAliasRule(string CategorySlug, string[] Terms);
}

public sealed record CategoryResolutionResult
{
    public string? CategorySlug { get; init; }
    public string ResolutionSource { get; init; } = "unresolved";
    public bool WasInferred { get; init; }
    public List<string> Diagnostics { get; init; } = [];

    public static CategoryResolutionResult Explicit(string categorySlug) => new()
    {
        CategorySlug = categorySlug,
        ResolutionSource = "explicit",
        WasInferred = false,
        Diagnostics = [$"Using explicit category slug `{categorySlug}`."],
    };

    public static CategoryResolutionResult Inferred(string categorySlug, string source, string diagnostic) => new()
    {
        CategorySlug = categorySlug,
        ResolutionSource = source,
        WasInferred = true,
        Diagnostics = [diagnostic],
    };

    public static CategoryResolutionResult Unresolved(params string[] diagnostics) => new()
    {
        ResolutionSource = "unresolved",
        WasInferred = false,
        Diagnostics = diagnostics
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
    };
}
