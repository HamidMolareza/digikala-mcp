using Digikala.Mcp.Domain.Enums;

namespace Digikala.Mcp.Application.Dtos;

public sealed record ToolErrorDto
{
    public bool Error => true;
    public ToolErrorCode Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? StaleData { get; init; }
}
