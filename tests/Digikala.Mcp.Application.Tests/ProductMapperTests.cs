using Digikala.Mcp.Application.Mapping;

namespace Digikala.Mcp.Application.Tests;

public class ProductMapperTests
{
    private readonly ProductMapper _mapper = new();

    [Fact]
    public void MapToSummary_ShouldThrow_WhenNotImplemented()
    {
        // Placeholder — will be replaced with real tests once mapping is implemented.
        Assert.Throws<NotImplementedException>(() =>
        {
            var element = System.Text.Json.JsonDocument.Parse("{}").RootElement;
            _mapper.MapToSummary(element);
        });
    }
}
