using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class BrowseByTagService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<SearchResultDto> ExecuteAsync(
        string tagSlug,
        int page = 1,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["tagSlug"] = tagSlug,
            ["page"] = page.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("browse_by_tag", ("tag", tagSlug), ("page", page));
        try
        {
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
                cache,
                cacheKey,
                CacheTier.MediumTerm,
                async token =>
                {
                    using var response = await apiClient.GetTagProductsAsync(tagSlug, page, token);
                    return mapper.MapToSearchResult(response);
                },
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new SearchResultDto
            {
                Warning = "Digikala tag browse request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "browse_by_tag",
                    "Digikala tag browse request failed.",
                    exception,
                    request),
                FilteredCount = 0,
                AppliedFilters = [],
            };
        }
    }
}
