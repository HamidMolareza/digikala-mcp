using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetBrandPageTool(IDigikalaApiClient apiClient, ICacheStore cache, BrandMapper mapper)
{
    [McpServerTool("get_brand_page"), Description("Get a premium brand landing page.")]
    public async Task<BrandPageDto> ExecuteAsync(
        [Description("Brand slug, e.g. 'persil'")] string brandSlug,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
