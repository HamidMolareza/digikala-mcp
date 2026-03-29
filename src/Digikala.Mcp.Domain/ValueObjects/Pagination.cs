namespace Digikala.Mcp.Domain.ValueObjects;

public sealed record Pagination(int CurrentPage, int TotalPages, int TotalItems);
