using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class PriceChartMapper
{
    public PriceChartDto MapToPriceChart(long productId, JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);
        var history = data.EnumerateArrayOrEmpty("price_chart")
            .Select(item => new
            {
                Date = ParseDate(item.GetStringOrNull("date")),
                Price = item.GetInt64OrNull("price") ?? 0,
            })
            .Where(item => item.Date is not null)
            .Select(item => new PricePointDto(item.Date.GetValueOrDefault(), item.Price))
            .ToList();

        var fallbackPrice = history.LastOrDefault()?.PriceRials ?? 0;
        var currentPrice = data.GetInt64OrNull("today", "selling_price") ?? fallbackPrice;

        return new PriceChartDto
        {
            ProductId = productId,
            CurrentPriceRials = currentPrice,
            LowestPriceRials = history.Count > 0 ? history.Min(item => item.PriceRials) : currentPrice,
            HighestPriceRials = history.Count > 0 ? history.Max(item => item.PriceRials) : currentPrice,
            History = history,
        };
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }

        return DateTimeOffset.TryParse(value, out var dateTimeOffset)
            ? DateOnly.FromDateTime(dateTimeOffset.UtcDateTime)
            : null;
    }
}
