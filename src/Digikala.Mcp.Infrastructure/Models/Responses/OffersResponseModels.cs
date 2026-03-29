using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class IncredibleOffersData
{
    [JsonPropertyName("incredible_products_list")]
    public List<ProductListItem>? IncredibleProductsList { get; set; }

    [JsonPropertyName("deal_of_the_day_products")]
    public List<ProductListItem>? DealOfTheDayProducts { get; set; }

    [JsonPropertyName("lightening_deal_products")]
    public List<ProductListItem>? LighteningDealProducts { get; set; }

    [JsonPropertyName("running_out_incredible_products")]
    public List<ProductListItem>? RunningOutProducts { get; set; }

    [JsonPropertyName("main_categories")]
    public List<CategoryRef>? MainCategories { get; set; }
}

public sealed class BestSellingData
{
    [JsonPropertyName("products")]
    public List<ProductListItem>? Products { get; set; }

    [JsonPropertyName("categories")]
    public List<CategoryRef>? Categories { get; set; }
}
