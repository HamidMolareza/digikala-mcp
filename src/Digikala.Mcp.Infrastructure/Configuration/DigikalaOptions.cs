namespace Digikala.Mcp.Infrastructure.Configuration;

public sealed class DigikalaOptions
{
    public const string SectionName = "Digikala";

    public string BaseUrl { get; set; } = "https://api.digikala.com";
    public string FeaturesBaseUrl { get; set; } = "https://api-zakaria.digikala.com";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRequestsPerSecond { get; set; } = 30;
    public string UserAgent { get; set; } = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:148.0) Gecko/20100101 Firefox/148.0";
    public int DefaultWarehouseId { get; set; } = 29;
}
