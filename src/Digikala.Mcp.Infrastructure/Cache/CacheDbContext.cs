using Microsoft.EntityFrameworkCore;

namespace Digikala.Mcp.Infrastructure.Cache;

public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CacheEntry>(entity =>
        {
            entity.ToTable("cache_entries");
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).HasColumnName("key");
            entity.Property(item => item.Value).HasColumnName("value");
            entity.Property(item => item.Tier).HasColumnName("tier");
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => item.ExpiresAt).HasDatabaseName("idx_cache_expires");
            entity.HasIndex(item => item.Tier).HasDatabaseName("idx_cache_tier");
        });
    }
}
