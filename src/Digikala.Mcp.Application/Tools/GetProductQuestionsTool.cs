using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductQuestionsTool(IDigikalaApiClient apiClient, ICacheStore cache, QuestionMapper mapper)
{
    [McpServerTool("get_product_questions"), Description("Get paginated Q&A for a product.")]
    public async Task<QuestionListDto> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
