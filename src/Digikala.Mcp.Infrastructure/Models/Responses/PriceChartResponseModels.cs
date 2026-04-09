using System.Text.Json.Serialization;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class PriceChartResponseData
{
    [JsonPropertyName("today")]
    public string? Today { get; set; }

    [JsonPropertyName("price_chart")]
    public List<PriceChartSeriesModel>? PriceChart { get; set; }
}

public sealed class PriceChartSeriesModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("history")]
    public List<PriceChartHistoryPointModel>? History { get; set; }
}

public sealed class PriceChartHistoryPointModel
{
    [JsonPropertyName("selling_price")]
    public long SellingPrice { get; set; }

    [JsonPropertyName("rrp_price")]
    public long? RrpPrice { get; set; }

    [JsonPropertyName("day")]
    public string? Day { get; set; }
}
