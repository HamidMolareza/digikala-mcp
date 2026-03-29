using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Cache;
using Digikala.Mcp.Infrastructure.Configuration;
using Digikala.Mcp.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digikala.Mcp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDigikalaInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DigikalaOptions>(configuration.GetSection(DigikalaOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddHttpClient<IDigikalaApiClient, DigikalaHttpClient>((sp, client) =>
        {
            var options = configuration
                .GetSection(DigikalaOptions.SectionName)
                .Get<DigikalaOptions>() ?? new DigikalaOptions();

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("X-Web-Optimize-Response", "1");
            client.DefaultRequestHeaders.Add("X-Web-Client", "desktop");
            client.DefaultRequestHeaders.Add("X-Web-Client-Id", "web");
            client.DefaultRequestHeaders.Add("Origin", "https://www.digikala.com");
            client.DefaultRequestHeaders.Add("Referer", "https://www.digikala.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<ICacheStore, SqliteCacheStore>();
        services.AddHostedService<CacheCleanupService>();

        return services;
    }
}
