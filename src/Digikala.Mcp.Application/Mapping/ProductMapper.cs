using System.Text.Json;
using Digikala.Mcp.Application.Dtos;

namespace Digikala.Mcp.Application.Mapping;

public sealed class ProductMapper
{
    public ProductSummaryDto MapToSummary(JsonElement productElement)
    {
        throw new NotImplementedException();
    }

    public List<ProductSummaryDto> MapToSummaryList(JsonElement productsArray)
    {
        throw new NotImplementedException();
    }

    public ProductDetailDto MapToDetail(JsonElement productElement)
    {
        throw new NotImplementedException();
    }

    public SearchResultDto MapToSearchResult(JsonDocument response)
    {
        throw new NotImplementedException();
    }
}
