namespace Digikala.Mcp.Application.Dtos;

public sealed record QuestionDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public long Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<AnswerDto> Answers { get; init; } = [];
}

public sealed record AnswerDto
{
    public long Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int LikeCount { get; init; }
}

public sealed record QuestionListDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public List<QuestionDto> Questions { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}
