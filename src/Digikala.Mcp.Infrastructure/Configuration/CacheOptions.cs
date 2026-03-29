namespace Digikala.Mcp.Infrastructure.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public string DatabasePath { get; set; } = "./cache.db";
    public int LongTermTtlMinutes { get; set; } = 1440;
    public int MediumTermTtlMinutes { get; set; } = 60;
    public int ShortTermTtlMinutes { get; set; } = 5;
    public int CleanupIntervalMinutes { get; set; } = 10;
}
