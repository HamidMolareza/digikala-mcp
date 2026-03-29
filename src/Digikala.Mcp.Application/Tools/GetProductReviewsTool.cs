using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductReviewsTool(IDigikalaApiClient apiClient, ICacheStore cache, ReviewMapper mapper)
{
    [McpServerTool("get_product_reviews"), Description("Get paginated user reviews for a product.")]
    public async Task<ReviewListDto> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
