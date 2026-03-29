namespace Digikala.Mcp.Infrastructure.Configuration;

public sealed class ServerLoggingOptions
{
    public const string SectionName = "ServerLogging";

    public string MinimumLevel { get; set; } = "Information";
    public string AuditLogPath { get; set; } = "./app-data/logs/audit-.clef";
    public string ErrorLogPath { get; set; } = "./app-data/logs/error-.clef";
    public bool EnableConsole { get; set; } = true;
}
