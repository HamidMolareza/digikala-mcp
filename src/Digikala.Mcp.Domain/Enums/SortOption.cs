namespace Digikala.Mcp.Domain.Enums;

/// <summary>
/// Product listing sort ids as reported by Digikala's live <c>sort_options</c>.
/// </summary>
public enum SortOption
{
    Newest = 1,
    MostViewed = 4,
    BestSelling = 7,
    Cheapest = 20,
    MostExpensive = 21,
    MostRelevant = 22,
    FastestShipping = 25,
    BuyersPick = 27,
    Featured = 29,
}
