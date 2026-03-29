using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Digikala.Mcp.Infrastructure.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDigikalaInfrastructure_ShouldResolveTypedClientMultipleTimes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Digikala:BaseUrl"] = "https://api.example.com",
                ["Digikala:FeaturesBaseUrl"] = "https://features.example.com",
                ["Cache:DatabasePath"] = Path.Combine(Path.GetTempPath(), $"digikala-test-{Guid.NewGuid():N}.db"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigikalaInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IDigikalaApiClient>();
        var second = provider.GetRequiredService<IDigikalaApiClient>();

        Assert.NotNull(first);
        Assert.NotNull(second);
    }
}
