namespace Digikala.Mcp.Domain.Entities;

public sealed class Question
{
    public long Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<Answer> Answers { get; init; } = [];
}

public sealed class Answer
{
    public long Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int LikeCount { get; init; }
}
