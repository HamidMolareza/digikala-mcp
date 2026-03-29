namespace Digikala.Mcp.Application.Dtos;

public sealed record ReviewDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
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

public sealed record ReviewListDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public List<ReviewDto> Reviews { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public double? AverageRating { get; init; }
}
