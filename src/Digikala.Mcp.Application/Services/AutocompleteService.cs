using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class AutocompleteService(IDigikalaApiClient apiClient, ICacheStore cache, AutocompleteMapper mapper)
{
    public async Task<AutocompleteDto> ExecuteAsync(
        string query,
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
