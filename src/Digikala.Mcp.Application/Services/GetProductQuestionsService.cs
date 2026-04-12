using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductQuestionsService(IDigikalaApiClient apiClient, ICacheStore cache, QuestionMapper mapper)
{
    public async Task<QuestionListDto> ExecuteAsync(
        long productId,
        int page = 1,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["page"] = page.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_questions", ("productId", productId), ("page", page));
        try
        {
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
                cache,
                cacheKey,
                CacheTier.ShortTerm,
                async token =>
                {
                    using var response = await apiClient.GetProductQuestionsAsync(productId, page, ct: token);
                    return mapper.MapToQuestionList(response);
                },
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new QuestionListDto
            {
                CurrentPage = page,
                Warning = "Digikala product questions request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_product_questions",
                    "Digikala product questions request failed.",
                    exception,
                    request),
            };
        }
    }
}
