using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class AutocompleteTool(IDigikalaApiClient apiClient, ICacheStore cache, AutocompleteMapper mapper)
{
    [McpServerTool("autocomplete"), Description("Get search autocomplete suggestions and trends for a query prefix.")]
    public async Task<AutocompleteDto> ExecuteAsync(
        [Description("Search query prefix")] string query,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
