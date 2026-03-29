using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class BrowseByTagTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("browse_by_tag"), Description("Browse products by SEO tag (e.g. 'football-boots', 'iphone-16').")]
    public async Task<SearchResultDto> ExecuteAsync(
        [Description("Tag slug, e.g. 'iphone-16'")] string tagSlug,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
