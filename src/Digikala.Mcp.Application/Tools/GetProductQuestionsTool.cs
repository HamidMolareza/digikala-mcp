using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductQuestionsTool(IDigikalaApiClient apiClient, ICacheStore cache, QuestionMapper mapper)
{
    [McpServerTool, Description("Get paginated Q&A for a product.")]
    public async Task<QuestionListDto> get_product_questions(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_questions", ("productId", productId), ("page", page));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductQuestionsAsync(productId, page, ct: token);
                return mapper.MapToQuestionList(response);
            },
            ct);
    }
}
