using System.ComponentModel.DataAnnotations;

namespace Digikala.Mcp.Infrastructure.Cache;

public sealed class CacheEntry
{
    [Key]
    [MaxLength(512)]
    public string Key { get; set; } = string.Empty;

    public byte[] Value { get; set; } = [];

    [MaxLength(32)]
    public string Tier { get; set; } = string.Empty;

    public long ExpiresAt { get; set; }

    public long CreatedAt { get; set; }
}
