using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class AutocompleteMapper
{
    public AutocompleteDto MapToAutocomplete(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new AutocompleteDto
        {
            Suggestions = data.EnumerateArrayOrEmpty("suggestions")
                .Select(item => new SuggestionDto
                {
                    Text = item.GetStringOrNull("title") ?? item.GetStringOrNull("keyword") ?? string.Empty,
                    CategorySlug = item.GetStringOrNull("category", "code"),
                    Url = item.GetUrl("url"),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                .ToList(),
            TrendingSearches = data.EnumerateArrayOrEmpty("trends")
                .Select(item => item.GetStringOrNull("title") ?? item.GetStringOrNull("keyword"))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Cast<string>()
                .ToList(),
        };
    }
}
