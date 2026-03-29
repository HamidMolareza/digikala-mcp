namespace Digikala.Mcp.Domain.Entities;

public sealed class Brand
{
    public int Id { get; init; }
    public string TitleFa { get; init; } = string.Empty;
    public string? TitleEn { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
}
