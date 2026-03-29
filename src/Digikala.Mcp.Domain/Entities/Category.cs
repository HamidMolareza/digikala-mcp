namespace Digikala.Mcp.Domain.Entities;

public sealed class Category
{
    public int Id { get; init; }
    public string TitleFa { get; init; } = string.Empty;
    public string? TitleEn { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? IconName { get; init; }
    public List<Category> Children { get; init; } = [];
}
