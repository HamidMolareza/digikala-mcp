using System.Text.Json;
using System.Collections.Generic;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class ProductMapper
{
    private const string DigikalaWebBaseUrl = "https://www.digikala.com";

    public ProductSummaryDto MapToSummary(JsonElement productElement)
    {
        if (productElement.ValueKind != JsonValueKind.Object)
        {
            return new ProductSummaryDto();
        }

        var sellingPrice = productElement.GetInt64OrNull("default_variant", "price", "selling_price") ?? 0;
        var originalPrice = productElement.GetInt64OrNull("default_variant", "price", "rrp_price");
        var relativeUrl = productElement.GetUrl("url") ?? string.Empty;
        var ratingCount = productElement.GetInt32OrNull("rating", "count") ?? 0;

        var summary = new ProductSummaryDto
        {
            Id = productElement.GetInt64OrNull("id") ?? 0,
            Title = productElement.GetStringOrNull("title_fa") ?? productElement.GetStringOrNull("title_en") ?? string.Empty,
            EnglishTitle = productElement.GetStringOrNull("title_en"),
            MainImageUrl = ExtractFirstImageUrl(productElement),
            PriceRials = sellingPrice,
            OriginalPriceRials = originalPrice,
            DiscountPercent = productElement.GetInt32OrNull("default_variant", "price", "discount_percent") ??
                              (sellingPrice > 0 && originalPrice > sellingPrice
                                  ? (int)Math.Round((originalPrice.Value - sellingPrice) * 100d / originalPrice.Value)
                                  : null),
            Rating = productElement.GetDoubleOrNull("rating", "rate"),
            RatingCount = ratingCount,
            ReviewCount = ratingCount,
            CategorySlug = productElement.GetStringOrNull("category", "code"),
            IsAvailable = JsonHelpers.IsAvailableStatus(productElement.GetStringOrNull("status")),
            Url = relativeUrl,
            AbsoluteUrl = BuildAbsoluteUrl(relativeUrl),
        };

        return ProductSuitabilityAnalyzer.EnrichSummary(summary);
    }

    public List<ProductSummaryDto> MapToSummaryList(JsonElement productsArray)
    {
        if (productsArray.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return productsArray.EnumerateArray()
            .Select(MapToSummary)
            .Where(item => item.Id != 0 || !string.IsNullOrWhiteSpace(item.Title))
            .ToList();
    }

    public ProductDetailDto MapToDetail(JsonElement productElement)
    {
        var summary = MapToSummary(productElement);
        var pros = productElement.EnumerateArrayOrEmpty("expert_reviews", "advantages")
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToList();
        var cons = productElement.EnumerateArrayOrEmpty("expert_reviews", "disadvantages")
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToList();

        var detail = new ProductDetailDto
        {
            Id = summary.Id,
            Title = summary.Title,
            EnglishTitle = summary.EnglishTitle,
            MainImageUrl = summary.MainImageUrl,
            PriceRials = summary.PriceRials,
            OriginalPriceRials = summary.OriginalPriceRials,
            DiscountPercent = summary.DiscountPercent,
            Rating = summary.Rating,
            ReviewCount = summary.ReviewCount,
            CategorySlug = summary.CategorySlug,
            IsAvailable = summary.IsAvailable,
            Url = summary.Url,
            Description = productElement.GetStringOrNull("expert_reviews", "description"),
            Brand = productElement.GetStringOrNull("brand", "title_fa") ?? productElement.GetStringOrNull("brand", "title_en"),
            BrandSlug = productElement.GetStringOrNull("brand", "code"),
            Seller = productElement.GetStringOrNull("default_variant", "seller", "title"),
            SellerRating = productElement.GetDoubleOrNull("default_variant", "seller", "rating") ??
                           productElement.GetDoubleOrNull("default_variant", "seller", "rating", "total_rate"),
            Warranty = productElement.GetStringOrNull("default_variant", "warranty", "title_fa"),
            Specifications = productElement.EnumerateArrayOrEmpty("specifications")
                .SelectMany(group => group.EnumerateArrayOrEmpty("attributes")
                    .Select(attribute => new SpecDto(
                        group.GetStringOrNull("title") ?? string.Empty,
                        attribute.GetStringOrNull("title") ?? string.Empty,
                        string.Join(", ", attribute.EnumerateArrayOrEmpty("values")
                            .Select(item => item.GetString())
                            .Where(item => !string.IsNullOrWhiteSpace(item))))))
                .Where(item => !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Value))
                .ToList(),
            ImageUrls = ExtractImageUrls(productElement)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            Variants = productElement.EnumerateArrayOrEmpty("variants")
                .Select(item => new VariantDto
                {
                    Id = item.GetInt64OrNull("id") ?? 0,
                    Title = item.GetStringOrNull("color", "title") ??
                            item.GetStringOrNull("seller", "title") ??
                            item.GetStringOrNull("warranty", "title_fa"),
                    PriceRials = item.GetInt64OrNull("price", "selling_price") ?? 0,
                    IsAvailable = (item.GetInt64OrNull("price", "selling_price") ?? 0) > 0,
                    ColorHex = item.GetStringOrNull("color", "hex_code"),
                })
                .Where(item => item.Id != 0)
                .ToList(),
            ProsCons = pros.Count > 0 || cons.Count > 0 ? new ProsConsDto(pros, cons) : null,
        };

        return ProductSuitabilityAnalyzer.EnrichDetail(detail);
    }

    public SearchResultDto MapToSearchResult(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new SearchResultDto
        {
            Products = MapToSummaryList(data.TryGetNested(out var products, "products") ? products : default),
            TotalCount = data.GetInt32OrNull("pager", "total_items") ?? 0,
            CurrentPage = data.GetInt32OrNull("pager", "current_page") ?? 1,
            TotalPages = data.GetInt32OrNull("pager", "total_pages") ?? 1,
            AvailableFilters = data.EnumerateArrayOrEmpty("filters")
                .Select(group => MapFilterGroup(group))
                .Concat(MapFilterGroupsFromObject(data))
                .Where(group => !string.IsNullOrWhiteSpace(group.Title) || !string.IsNullOrWhiteSpace(group.Code))
                .ToList(),
            AvailableSortOptions = data.EnumerateArrayOrEmpty("sort_options")
                .Select(item => item.GetStringOrNull("title_fa"))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Cast<string>()
                .ToList(),
        };
    }

    private static string? ExtractFirstImageUrl(JsonElement productElement)
    {
        return productElement.GetUrl("images");
    }

    private static IEnumerable<string> ExtractImageUrls(JsonElement productElement)
    {
        if (!productElement.TryGetNested(out var images, "images"))
        {
            return [];
        }

        return EnumerateUrls(images);
    }

    private static IEnumerable<string> EnumerateUrls(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                {
                    var url = value.GetString();
                    return string.IsNullOrWhiteSpace(url) ? [] : [url];
                }

            case JsonValueKind.Array:
                return value.EnumerateArray().SelectMany(EnumerateUrls);

            case JsonValueKind.Object:
                if (value.TryGetProperty("url", out var nestedUrl))
                {
                    return EnumerateUrls(nestedUrl);
                }

                return value.EnumerateObject().SelectMany(property => EnumerateUrls(property.Value));

            default:
                return [];
        }
    }

    private static IEnumerable<FilterGroupDto> MapFilterGroupsFromObject(JsonElement data)
    {
        if (!data.TryGetNested(out var filters, "filters") || filters.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return filters.EnumerateObject().Select(property => MapFilterGroup(property.Value, property.Name));
    }

    private static FilterGroupDto MapFilterGroup(JsonElement group, string? fallbackCode = null)
    {
        return new FilterGroupDto
        {
            Title = group.GetStringOrNull("title_fa") ?? group.GetStringOrNull("title") ?? string.Empty,
            Code = group.GetStringOrNull("code") ?? fallbackCode ?? string.Empty,
            Options = group.EnumerateArrayOrEmpty("options")
                .Select(option => new FilterOptionDto
                {
                    Title = option.GetStringOrNull("title_fa") ?? option.GetStringOrNull("title") ?? string.Empty,
                    Value = option.GetStringOrNull("id") ?? option.GetStringOrNull("value") ?? option.GetStringOrNull("code") ?? string.Empty,
                    Count = option.GetInt32OrNull("count") ?? 0,
                })
                .Where(option => !string.IsNullOrWhiteSpace(option.Title))
                .ToList(),
        };
    }

    private static string BuildAbsoluteUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return absoluteUri.ToString();
        }

        var relativePath = url.StartsWith("/", StringComparison.Ordinal) ? url : $"/{url}";
        return $"{DigikalaWebBaseUrl}{relativePath}";
    }
}
