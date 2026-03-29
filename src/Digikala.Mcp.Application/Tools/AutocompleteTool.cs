using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class AutocompleteTool(IDigikalaApiClient apiClient, ICacheStore cache, AutocompleteMapper mapper)
{
    [McpServerTool, Description("Get search autocomplete suggestions and trends for a query prefix.")]
    public async Task<AutocompleteDto> autocomplete(
        [Description("Search query prefix")] string query,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("autocomplete", ("q", query));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetAutocompleteAsync(query, token);
                return mapper.MapToAutocomplete(response);
            },
            ct);
    }
}
