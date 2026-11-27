using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class BrandMapper(ProductMapper productMapper)
{
    public const int MaxProductsPerList = 10;

    /// <summary>
    /// Builds a brand page from two brand product listings, since Digikala retired /v1/brands/{slug}/premium/.
    /// </summary>
    public BrandPageDto MapToBrandPage(JsonDocument newestResponse, JsonDocument bestSellingResponse)
    {
        var newest = JsonHelpers.GetData(newestResponse);
        var bestSelling = JsonHelpers.GetData(bestSellingResponse);
        var brand = newest.TryGetNested(out var newestBrand, "brand") ? newestBrand
            : bestSelling.TryGetNested(out var bestSellingBrand, "brand") ? bestSellingBrand
            : default;

        return new BrandPageDto
        {
            Id = brand.GetInt32OrNull("id") ?? 0,
            Title = brand.GetStringOrNull("title_fa") ?? brand.GetStringOrNull("title_en") ?? string.Empty,
            Slug = brand.GetStringOrNull("code") ?? string.Empty,
            LogoUrl = brand.GetUrl("logo", "url"),
            NewestProducts = MapProducts(newest),
            BestSellingProducts = MapProducts(bestSelling),
        };
    }

    private List<ProductSummaryDto> MapProducts(JsonElement data) =>
        ProductListing.Unwrap(data).Products
            .Select(productMapper.MapToSummary)
            .Where(item => item.Id != 0 || !string.IsNullOrWhiteSpace(item.Title))
            .Take(MaxProductsPerList)
            .ToList();
}
