using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class OfferMapper
{
    public OffersListDto MapToOffersList(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);
        var products = data.EnumerateArrayOrEmpty("products").ToList();

        if (products.Count == 0)
        {
            products = data.EnumerateArrayOrEmpty("incredible_products_list")
                .Concat(data.EnumerateArrayOrEmpty("deal_of_the_day_products"))
                .Concat(data.EnumerateArrayOrEmpty("lightening_deal_products"))
                .GroupBy(item => item.GetInt64OrNull("id") ?? 0)
                .Select(group => group.First())
                .ToList();
        }

        return new OffersListDto
        {
            Offers = products.Select(MapOffer).ToList(),
            CurrentPage = data.GetInt32OrNull("pager", "current_page") ?? 1,
            TotalPages = data.GetInt32OrNull("pager", "total_pages") ?? 1,
        };
    }

    private static OfferDto MapOffer(JsonElement element)
    {
        var sellingPrice = element.GetInt64OrNull("default_variant", "price", "selling_price") ?? 0;
        var originalPrice = element.GetInt64OrNull("default_variant", "price", "rrp_price");

        return new OfferDto
        {
            ProductId = element.GetInt64OrNull("id") ?? 0,
            Title = element.GetStringOrNull("title_fa") ?? element.GetStringOrNull("title_en") ?? string.Empty,
            MainImageUrl = ExtractFirstImageUrl(element),
            SellingPriceRials = sellingPrice,
            OriginalPriceRials = originalPrice,
            DiscountPercent = element.GetInt32OrNull("default_variant", "price", "discount_percent") ??
                              (sellingPrice > 0 && originalPrice > sellingPrice
                                  ? (int)Math.Round((originalPrice.Value - sellingPrice) * 100d / originalPrice.Value)
                                  : null),
            Url = element.GetUrl("url") ?? string.Empty,
        };
    }

    private static string? ExtractFirstImageUrl(JsonElement productElement)
    {
        foreach (var image in productElement.EnumerateArrayOrEmpty("images"))
        {
            var url = image.GetUrl("url");
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }
}
