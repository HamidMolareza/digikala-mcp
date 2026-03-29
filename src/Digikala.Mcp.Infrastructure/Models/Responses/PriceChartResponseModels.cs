using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class PriceChartResponseData
{
    [JsonPropertyName("today")]
    public PriceTodayModel? Today { get; set; }

    [JsonPropertyName("price_chart")]
    public List<PriceChartPointModel>? PriceChart { get; set; }
}

public sealed class PriceTodayModel
{
    [JsonPropertyName("selling_price")]
    public long SellingPrice { get; set; }

    [JsonPropertyName("rrp_price")]
    public long? RrpPrice { get; set; }
}

public sealed class PriceChartPointModel
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("price")]
    public long Price { get; set; }
}
