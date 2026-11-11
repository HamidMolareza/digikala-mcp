namespace Digikala.Mcp.Domain.ValueObjects;

/// <summary>
/// Upstream product search request. A null <see cref="CategorySlug"/> means a global keyword search.
/// </summary>
public sealed record ProductSearchRequest(
    string Query,
    string? CategorySlug = null,
    int Page = 1,
    int? Sort = null,
    long? MinPriceRials = null,
    long? MaxPriceRials = null,
    bool OnlyAvailable = false);
