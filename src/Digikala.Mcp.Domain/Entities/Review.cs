namespace Digikala.Mcp.Domain.Entities;

public sealed class Review
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? UserName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int LikeCount { get; init; }
    public int DislikeCount { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public List<string> Pros { get; init; } = [];
    public List<string> Cons { get; init; } = [];
}
