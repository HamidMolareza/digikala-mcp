using System.Text.Json;
using System.Text.RegularExpressions;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed partial class AutocompleteMapper
{
    public AutocompleteDto MapToAutocomplete(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        // /discovery/api/v2/autocomplete returns "auto_complete"; the retired /v1/autocomplete/ returned "suggestions".
        var suggestions = data.EnumerateArrayOrEmpty("auto_complete")
            .Concat(data.EnumerateArrayOrEmpty("suggestions"))
            .Select(MapSuggestion)
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .ToList();

        return new AutocompleteDto
        {
            Suggestions = suggestions,
            TrendingSearches = data.EnumerateArrayOrEmpty("trends")
                .Select(item => item.GetStringOrNull("title") ?? item.GetStringOrNull("keyword"))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .ToList(),
        };
    }

    private static SuggestionDto MapSuggestion(JsonElement item)
    {
        var url = item.GetUrl("url");
        return new SuggestionDto
        {
            Text = item.GetStringOrNull("title") ?? item.GetStringOrNull("keyword") ?? string.Empty,
            Type = item.GetStringOrNull("type"),
            CategoryTitle = item.GetStringOrNull("category_title"),
            CategorySlug = item.GetStringOrNull("category", "code") ?? ExtractCategorySlug(url),
            Url = url,
        };
    }

    private static string? ExtractCategorySlug(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var match = CategoryUrlRegex().Match(url);
        return match.Success ? match.Groups["slug"].Value : null;
    }

    [GeneratedRegex(@"/search/category-(?<slug>[^/?#]+)")]
    private static partial Regex CategoryUrlRegex();
}
