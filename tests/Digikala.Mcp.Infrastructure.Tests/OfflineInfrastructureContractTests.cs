using System.Text.Json;
using Digikala.Mcp.Infrastructure.Models.Common;
using Digikala.Mcp.Infrastructure.Models.Responses;
using Xunit;

namespace Digikala.Mcp.Infrastructure.Tests;

public sealed class OfflineInfrastructureContractTests
{
    [Fact]
    public void DynamicLandingMobileFixture_ShouldDeserializeLandingAndWidgetReferences()
    {
        using var response = LoadFixture("dynamic-landing-mobile.json");

        var envelope = DeserializeEnvelope<DynamicLandingMobileData>(response);

        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.Equal(1591, envelope.Data!.LandingId);
        Assert.False(envelope.Data.LoggedIn);
        Assert.Equal("noindex, nofollow", envelope.Data.Seo!.Header!.RobotsMeta);

        var widgets = Assert.IsAssignableFrom<List<DynamicLandingWidgetModel>>(envelope.Data.Widgets);

        var heroWidget = Assert.Single(widgets, widget => widget.WidgetType == "hero");
        Assert.Equal(9369, heroWidget.WidgetId);
        Assert.Equal("/product/dkp-21462684?camCode=1591", Assert.Single(heroWidget.Data!.MobileSlides!).Url!.Uri);

        var linkedWidget = Assert.Single(widgets, widget => widget.WidgetType == "special_carousel");
        Assert.Equal("v1/dynamic-landing/widget/9370/", linkedWidget.Data!.WidgetUrl);

        var offerWidget = Assert.Single(widgets, widget => widget.WidgetType == "incredible_offer");
        Assert.Equal("/incredible-offers/?category_id=11&camCode=1591", offerWidget.Data!.SeeMoreUrl!.Uri);
        Assert.Equal(14423707, Assert.Single(offerWidget.Data.Products!).Id);
    }

    [Fact]
    public void MagnetHomePageFixture_ShouldDeserializePostsAndAttachedProducts()
    {
        using var response = LoadFixture("magnet-home-page.json");

        var envelope = DeserializeEnvelope<MagnetHomePageData>(response);

        Assert.Equal(200, envelope.Status);
        var post = Assert.Single(envelope.Data!.Posts!);
        Assert.Equal("84511336332", post.Id);
        Assert.Equal("post", post.Type);
        Assert.Equal("verified", post.Status);
        Assert.Equal("191factory", post.Author!.Username);
        Assert.Equal("video", Assert.Single(post.Media!).Type);
        Assert.Equal("نجاری", Assert.Single(post.Tags!, tag => tag.Id == 21533).Title);
        Assert.Equal(5215726, Assert.Single(post.Products!, product => product.Id == 5215726).Id);
    }

    private static JsonDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiContracts", fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static DigikalaApiResponse<T> DeserializeEnvelope<T>(JsonDocument response)
    {
        var envelope = JsonSerializer.Deserialize<DigikalaApiResponse<T>>(response.RootElement.GetRawText());
        Assert.NotNull(envelope);
        return envelope!;
    }
}
