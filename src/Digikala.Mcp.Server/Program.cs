using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Services;
using Digikala.Mcp.Infrastructure.Configuration;
using Digikala.Mcp.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;

var builder = Host.CreateApplicationBuilder(args);

var loggingOptions = builder.Configuration
    .GetSection(ServerLoggingOptions.SectionName)
    .Get<ServerLoggingOptions>() ?? new ServerLoggingOptions();

EnsureDirectoryFor(loggingOptions.AuditLogPath);
EnsureDirectoryFor(loggingOptions.ErrorLogPath);

builder.Logging.ClearProviders();
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Is(ParseLevel(loggingOptions.MinimumLevel))
        .Enrich.FromLogContext();

    if (loggingOptions.EnableConsole)
    {
        loggerConfiguration.WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose);
    }

    loggerConfiguration.WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.FromSource("Digikala.Mcp.Server.Tools"))
        .WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: loggingOptions.AuditLogPath,
            rollingInterval: RollingInterval.Day,
            shared: true));

    loggerConfiguration.WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt => evt.Level >= LogEventLevel.Error)
        .WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: loggingOptions.ErrorLogPath,
            rollingInterval: RollingInterval.Day,
            shared: true));
});

builder.Services.AddDigikalaInfrastructure(builder.Configuration);

builder.Services.AddSingleton<ProductMapper>();
builder.Services.AddSingleton<ReviewMapper>();
builder.Services.AddSingleton<QuestionMapper>();
builder.Services.AddSingleton<PriceChartMapper>();
builder.Services.AddSingleton<CategoryMapper>();
builder.Services.AddSingleton<OfferMapper>();
builder.Services.AddSingleton<AutocompleteMapper>();
builder.Services.AddSingleton<BrandMapper>();

builder.Services.AddSingleton<SearchProductsService>();
builder.Services.AddSingleton<GetProductDetailService>();
builder.Services.AddSingleton<GetProductFeedbackSummaryService>();
builder.Services.AddSingleton<GetProductOverviewService>();
builder.Services.AddSingleton<GetProductPriceChartService>();
builder.Services.AddSingleton<GetProductReviewsService>();
builder.Services.AddSingleton<GetProductQuestionsService>();
builder.Services.AddSingleton<GetProductRecommendationsService>();
builder.Services.AddSingleton<GetBestSellingService>();
builder.Services.AddSingleton<GetIncredibleOffersService>();
builder.Services.AddSingleton<GetTrendingProductsService>();
builder.Services.AddSingleton<GetCategoryLandingService>();
builder.Services.AddSingleton<BrowseByTagService>();
builder.Services.AddSingleton<AutocompleteService>();
builder.Services.AddSingleton<GetBrandPageService>();
builder.Services.AddSingleton<GetSellerVouchersService>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "digikala-mcp",
            Version = "0.1.0",
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Digikala.Mcp.Server.Tools.SearchProductsTool).Assembly);

var app = builder.Build();
await app.RunAsync();

static void EnsureDirectoryFor(string path)
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(path));
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static LogEventLevel ParseLevel(string? level) =>
    Enum.TryParse<LogEventLevel>(level, true, out var parsed)
        ? parsed
        : LogEventLevel.Information;
