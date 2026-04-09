using System.Text.Json;
using System.Globalization;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class PriceChartMapper
{
    public PriceChartDto MapToPriceChart(long productId, JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);
        var history = EnumerateHistoryPoints(data)
            .Select(item => new
            {
                Date = ParseDate(item.Date),
                Price = item.Price,
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

    private static IEnumerable<(string? Date, long Price)> EnumerateHistoryPoints(JsonElement data)
    {
        foreach (var item in data.EnumerateArrayOrEmpty("price_chart"))
        {
            var directPrice = item.GetInt64OrNull("price");
            var directDate = item.GetStringOrNull("date");
            if (directPrice is not null || directDate is not null)
            {
                yield return (directDate, directPrice ?? 0);
                continue;
            }

            foreach (var historyItem in item.EnumerateArrayOrEmpty("history"))
            {
                yield return (
                    historyItem.GetStringOrNull("day") ?? historyItem.GetStringOrNull("date"),
                    historyItem.GetInt64OrNull("selling_price") ?? historyItem.GetInt64OrNull("price") ?? 0);
            }
        }
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

        if (TryParsePersianDate(value, out var persianDate))
        {
            return persianDate;
        }

        return DateTimeOffset.TryParse(value, out var dateTimeOffset)
            ? DateOnly.FromDateTime(dateTimeOffset.UtcDateTime)
            : null;
    }

    private static bool TryParsePersianDate(string value, out DateOnly date)
    {
        date = default;
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 3 ||
            !int.TryParse(segments[0], out var year) ||
            !int.TryParse(segments[1], out var month) ||
            !int.TryParse(segments[2], out var day))
        {
            return false;
        }

        try
        {
            var calendar = new PersianCalendar();
            date = DateOnly.FromDateTime(calendar.ToDateTime(year, month, day, 0, 0, 0, 0));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
