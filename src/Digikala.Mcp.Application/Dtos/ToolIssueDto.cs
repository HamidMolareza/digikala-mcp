namespace Digikala.Mcp.Application.Dtos;

public sealed record ToolIssueDto
{
    public bool Error { get; init; } = true;
    public string Code { get; init; } = "UpstreamError";
    public string Message { get; init; } = string.Empty;
    public string Tool { get; init; } = string.Empty;
    public Dictionary<string, string?> Request { get; init; } = [];
    public string? UpstreamEndpoint { get; init; }
    public int? StatusCode { get; init; }
    public bool Retryable { get; init; }
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("n");
    public List<string> Diagnostics { get; init; } = [];
}
