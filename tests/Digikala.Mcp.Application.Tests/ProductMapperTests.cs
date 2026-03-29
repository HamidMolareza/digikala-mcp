using System.Text.Json;
using Digikala.Mcp.Application.Mapping;
using Xunit;

namespace Digikala.Mcp.Application.Tests;

public class ProductMapperTests
{
    private readonly ProductMapper _mapper = new();

    [Fact]
    public void MapToSummary_ShouldMapCommonProductFields()
    {
        using var document = JsonDocument.Parse("""
            {
              "id": 123,
              "title_fa": "گوشی موبایل",
              "title_en": "Phone",
              "url": { "uri": "/product/dkp-123" },
              "images": [
                { "url": ["https://example.com/main.jpg"] }
              ],
              "status": "marketable",
              "rating": { "rate": 4.2, "count": 12 },
              "category": { "code": "mobile-phone" },
              "default_variant": {
                "price": {
                  "selling_price": 100000,
                  "rrp_price": 125000,
                  "discount_percent": 20
                }
              }
            }
            """);

        var result = _mapper.MapToSummary(document.RootElement);

        Assert.Equal(123, result.Id);
        Assert.Equal("گوشی موبایل", result.Title);
        Assert.Equal("Phone", result.EnglishTitle);
        Assert.Equal("https://example.com/main.jpg", result.MainImageUrl);
        Assert.Equal(100000, result.PriceRials);
        Assert.Equal(125000, result.OriginalPriceRials);
        Assert.Equal(20, result.DiscountPercent);
        Assert.Equal(4.2, result.Rating);
        Assert.Equal(12, result.RatingCount);
        Assert.Equal(12, result.ReviewCount);
        Assert.Equal("mobile-phone", result.CategorySlug);
        Assert.True(result.IsAvailable);
        Assert.Equal("/product/dkp-123", result.Url);
        Assert.Equal("https://www.digikala.com/product/dkp-123", result.AbsoluteUrl);
    }

    [Fact]
    public void MapToDetail_ShouldExtractSuitabilitySignals()
    {
        using var document = JsonDocument.Parse("""
            {
              "id": 456,
              "title_fa": "شامپو روزانه بدون سولفات برای موهای چرب و پوست سر حساس",
              "url": { "uri": "/product/dkp-456" },
              "images": [],
              "status": "marketable",
              "default_variant": { "price": { "selling_price": 100000 } },
              "expert_reviews": {
                "description": "فرمول ملایم و آبرسان با آلوئه ورا و بدون پارابن",
                "advantages": ["مناسب استفاده روزانه", "متعادل کننده چربی"]
              },
              "specifications": [
                {
                  "title": "مشخصات",
                  "attributes": [
                    { "title": "ویژگی", "values": ["بدون سولفات", "بدون پارابن", "حاوی آلوئه ورا"] }
                  ]
                }
              ]
            }
            """);

        var result = _mapper.MapToDetail(document.RootElement);

        Assert.True(result.IsDailyUse);
        Assert.True(result.IsSulfateFree);
        Assert.True(result.IsParabenFree);
        Assert.Contains("oily hair", result.SuitableForHairTypes);
        Assert.Contains("sensitive scalp", result.SuitableForScalpTypes);
        Assert.Contains("آلوئه ورا", result.KeyIngredients);
        Assert.Contains("balanced cleansing", result.UseCaseHints);
    }
}
