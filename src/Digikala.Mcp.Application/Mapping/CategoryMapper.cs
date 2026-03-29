using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class CategoryMapper(ProductMapper productMapper)
{
    public CategoryDto MapToCategory(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new CategoryDto
        {
            Id = data.GetInt32OrNull("category", "id") ?? 0,
            Title = data.GetStringOrNull("category", "title_fa") ?? data.GetStringOrNull("category", "title_en") ?? string.Empty,
            Slug = data.GetStringOrNull("category", "code") ?? string.Empty,
            Url = data.GetUrl("category", "url"),
            SubCategories = data.EnumerateArrayOrEmpty("sub_categories")
                .Select(item => new SubCategoryDto(
                    item.GetInt32OrNull("id") ?? 0,
                    item.GetStringOrNull("title_fa") ?? string.Empty,
                    item.GetStringOrNull("code") ?? string.Empty))
                .ToList(),
            BestSellingProducts = productMapper.MapToSummaryList(data.TryGetNested(out var bestSelling, "best_selling_products") ? bestSelling : default),
            PopularBrands = data.EnumerateArrayOrEmpty("popular_brands")
                .Select(item => new BrandSummaryDto(
                    item.GetInt32OrNull("id") ?? 0,
                    item.GetStringOrNull("title_fa") ?? item.GetStringOrNull("title_en") ?? string.Empty,
                    item.GetStringOrNull("code") ?? string.Empty,
                    item.GetUrl("logo", "url")))
                .ToList(),
        };
    }
}
