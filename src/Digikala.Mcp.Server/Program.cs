using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Application.Tools;
using Digikala.Mcp.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
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
    .WithToolsFromAssembly(typeof(SearchProductsTool).Assembly);

var app = builder.Build();
await app.RunAsync();
