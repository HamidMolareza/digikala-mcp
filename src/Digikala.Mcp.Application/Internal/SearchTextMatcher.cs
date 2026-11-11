using System.Text.RegularExpressions;

namespace Digikala.Mcp.Application.Internal;

internal static partial class SearchTextMatcher
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "و", "با", "برای", "از", "به", "در", "که", "یا", "مدل", "کد",
        "the", "a", "an", "and", "or", "for", "with", "of", "to", "in",
    };

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return WordRegex().Matches(Normalize(text))
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Share of query tokens that appear in any of the given texts (0 to 1).</summary>
    public static double MatchRatio(IReadOnlyList<string> queryTokens, params string?[] texts)
    {
        if (queryTokens.Count == 0)
        {
            return 0d;
        }

        var haystack = string.Join(' ', texts.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => Normalize(item!)));
        if (haystack.Length == 0)
        {
            return 0d;
        }

        var matched = queryTokens.Count(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
        return (double)matched / queryTokens.Count;
    }

    // Unifies Arabic/Persian letter variants and zero-width joiners so "دو‌جداره" matches "دوجداره".
    public static string Normalize(string text) =>
        text
            .Replace('ي', 'ی')
            .Replace('ى', 'ی')
            .Replace('ك', 'ک')
            .Replace('ة', 'ه')
            .Replace("‌", string.Empty)
            .Replace("‏", string.Empty)
            .ToLowerInvariant();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();
}
