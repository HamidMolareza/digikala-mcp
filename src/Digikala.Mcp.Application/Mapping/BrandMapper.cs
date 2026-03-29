using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class BrandMapper(ProductMapper productMapper)
{
    public BrandPageDto MapToBrandPage(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new BrandPageDto
        {
            Id = data.GetInt32OrNull("brand", "id") ?? 0,
            Title = data.GetStringOrNull("brand", "title_fa") ?? data.GetStringOrNull("brand", "title_en") ?? string.Empty,
            Slug = data.GetStringOrNull("brand", "code") ?? string.Empty,
            LogoUrl = data.GetUrl("brand", "logo", "url"),
            NewestProducts = productMapper.MapToSummaryList(data.TryGetNested(out var newest, "newest_products") ? newest : default),
            BestSellingProducts = productMapper.MapToSummaryList(data.TryGetNested(out var bestSelling, "best_selling_products") ? bestSelling : default),
        };
    }
}
