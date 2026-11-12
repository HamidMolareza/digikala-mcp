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

    /// <summary>
    /// Builds a landing summary from a best-selling category search, since Digikala retired /v1/categories/{slug}/.
    /// Popular brands are counted from the best sellers because the search's brand filter lists unrelated brands.
    /// </summary>
    public CategoryDto MapToCategoryFromSearch(JsonDocument response, int maxProducts = 10, int maxBrands = 8)
    {
        var data = JsonHelpers.GetData(response);
        var products = data.EnumerateArrayOrEmpty("products").ToList();
        var slug = data.GetStringOrNull("category", "code") ?? string.Empty;

        return new CategoryDto
        {
            Id = data.GetInt32OrNull("category", "id") ?? 0,
            Title = data.GetStringOrNull("category", "title_fa") ?? data.GetStringOrNull("category", "title_en") ?? string.Empty,
            Slug = slug,
            Url = string.IsNullOrEmpty(slug) ? null : $"/search/category-{slug}/",
            BestSellingProducts = products
                .Take(maxProducts)
                .Select(productMapper.MapToSummary)
                .Where(item => item.Id != 0)
                .ToList(),
            PopularBrands = products
                .Select(item => item.GetStringOrNull("data_layer", "brand")?.Trim())
                .Where(brand => !string.IsNullOrWhiteSpace(brand) && brand != MiscellaneousBrandTitle)
                .GroupBy(brand => brand!, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .Take(maxBrands)
                .Select(group => new BrandSummaryDto(0, group.Key, string.Empty, null))
                .ToList(),
        };
    }

    private const string MiscellaneousBrandTitle = "متفرقه";
}
